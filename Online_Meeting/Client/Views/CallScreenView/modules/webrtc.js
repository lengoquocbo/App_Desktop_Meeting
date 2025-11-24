import { LOCAL_ID } from './config.js';
import { getLocalParticipant, updateParticipantMedia, participantsById } from './participants.js';
import { postToHost } from './messaging.js';
import { renderVideoGrid } from './ui.js';
import { meetingContext } from './state.js';

// CONFIG
const RTC_CONFIG = {
    iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { urls: 'stun:stun1.l.google.com:19302' }
    ],
    sdpSemantics: 'unified-plan'
};

const peerConnections = new Map();
const pendingIceCandidates = new Map();
const lastStats = new Map(); // Map connection id of other participant with Key: connectionId, Value: { timestamp, bytesSent, bytesReceived }

//  TẠO KẾT NỐI
export async function createPeerConnection(connectionId, userId, username, shouldCreateOffer = false) {
    if (peerConnections.has(connectionId)) return peerConnections.get(connectionId);

    console.log(`Creating PC for ${username} (Offer: ${shouldCreateOffer})`);

    const pc = new RTCPeerConnection(RTC_CONFIG);

    const peerInfo = { pc, userId, username, connectionId };
    peerConnections.set(connectionId, peerInfo);

    // Gắn sự kiện lắng nghe
    setupHandlers(pc, peerInfo);

    // THÊM TRACK CỦA MÌNH VÀO NGAY LẬP TỨC
    // (Không chờ đợi, có gì thêm nấy để đảm bảo SDP có thông tin video/audio)
    const local = getLocalParticipant();
    if (local && local.stream) {
        local.stream.getTracks().forEach(track => {
            console.log(`Adding local track: ${track.kind}`);
            pc.addTrack(track, local.stream);
        });
    } else {
        console.warn("⚠️ Creating PC without local stream (Audio/Video might be missing)");
    }

    // XỬ LÝ ICE
    if (pendingIceCandidates.has(connectionId)) {
        pendingIceCandidates.get(connectionId).forEach(c => pc.addIceCandidate(new RTCIceCandidate(c)).catch(e => { }));
        pendingIceCandidates.delete(connectionId);
    }

    // CHỈ NGƯỜI MỚI (JOINER) MỚI TẠO OFFER
    if (shouldCreateOffer) {
        try {
            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);

            postToHost({
                type: 'send-offer',
                toConnectionId: connectionId,
                offer: { type: offer.type, sdp: offer.sdp }
            });
        } catch (err) {
            console.error("Create Offer Error:", err);
        }
    }

    return peerInfo;
}

// XỬ LÝ SỰ KIỆN
function setupHandlers(pc, peerInfo) {
    const { connectionId, username, userId } = peerInfo;

    // KHI CÓ STREAM TỪ NGƯỜI KHÁC
    pc.ontrack = (event) => {
        const stream = event.streams[0];
        if (!stream) return;
        console.log(`📺 Received stream from ${username}`);

        // Logic check user an toàn
        let p = participantsById.get(userId);
        if (!p) {
            // Fallback: nếu chưa có user, import tạo nóng
            import('./participants.js').then(mod => {
                mod.addRemoteParticipant({ id: userId, name: username, stream: stream });
                renderVideoGrid();
            });
        } else {
            // Logic đơn giản: cứ có track mới là update vào stream
            updateParticipantMedia(userId, { stream: stream });
            renderVideoGrid();
        }
    };

    // GỬI ICE CANDIDATE
    pc.onicecandidate = (event) => {
        if (event.candidate) {
            postToHost({
                type: 'send-ice-candidate',
                toConnectionId: connectionId,
                candidate: event.candidate.toJSON()
            });
        }
    };

    // TRẠNG THÁI KẾT NỐI (ĐÃ SỬA LỖI KICK OUT)
    pc.oniceconnectionstatechange = () => {
        console.log(`ICE State ${username}: ${pc.iceConnectionState}`);

        // Chỉ xóa khi thực sự 'closed' hoặc 'failed' lâu dài
        if (pc.iceConnectionState === 'closed') {
            removePeerConnection(connectionId);
        }
    };
}

// XỬ LÝ OFFER (NGƯỜI NHẬN)
export async function handleOffer(fromConnectionId, fromUserId, fromUsername, offer) {
    console.log(`📨 Received Offer from ${fromUsername}`);
    try {
        // Luôn get hoặc create PC mới
        let peerInfo = peerConnections.get(fromConnectionId);
        if (!peerInfo) {
            // False -> người nhận sẽ không tạo offer
            peerInfo = await createPeerConnection(fromConnectionId, fromUserId, fromUsername, false);
        }

        const pc = peerInfo.pc;

        // Set Remote
        await pc.setRemoteDescription(new RTCSessionDescription(offer));

        // Create Answer
        const answer = await pc.createAnswer();
        await pc.setLocalDescription(answer);

        // Gửi Answer
        postToHost({
            type: 'send-answer',
            toConnectionId: fromConnectionId,
            answer: { type: answer.type, sdp: answer.sdp }
        });

    } catch (err) {
        console.error("Handle Offer Error:", err);
    }
}

// XỬ LÝ ANSWER (NGƯỜI GỬI)
export async function handleAnswer(fromConnectionId, fromUserId, fromUsername, answer) {
    console.log(`📨 Received Answer from ${fromUsername}`);
    const peerInfo = peerConnections.get(fromConnectionId);
    if (peerInfo) {
        await peerInfo.pc.setRemoteDescription(new RTCSessionDescription(answer));
    }
}

// XỬ LÝ ICE
export async function handleIceCandidate(fromConnectionId, candidate) {
    const peerInfo = peerConnections.get(fromConnectionId);
    if (!peerInfo) {
        if (!pendingIceCandidates.has(fromConnectionId)) pendingIceCandidates.set(fromConnectionId, []);
        pendingIceCandidates.get(fromConnectionId).push(candidate);
    } else {
        peerInfo.pc.addIceCandidate(new RTCIceCandidate(candidate)).catch(e => { });
    }
}

// CÁC HÀM PHỤ TRỢ
export function removePeerConnection(connectionId) {
    const peerInfo = peerConnections.get(connectionId);
    if (peerInfo) {
        peerInfo.pc.close();
        peerConnections.delete(connectionId);
    }
}

export function handleParticipantJoined(connectionId, userId, username, shouldCreateOffer) {
    // Wrapper đơn giản
    return createPeerConnection(connectionId, userId, username, shouldCreateOffer);
}

export function handleParticipantLeft(connectionId) {
    removePeerConnection(connectionId);
}

//Share screen
export function updateLocalTracksInPeers() {

    const local = getLocalParticipant();
    if (!local || !local.stream) return;

    peerConnections.forEach(peerInfo => {
        const senders = peerInfo.pc.getSenders();
        const audioTrack = local.stream.getAudioTracks()[0];
        const videoTrack = local.stream.getVideoTracks()[0];

        senders.forEach(sender => {
            if (sender.track && sender.track.kind === 'audio' && audioTrack) {
                sender.replaceTrack(audioTrack).catch(e => { });
            }
            if (sender.track && sender.track.kind === 'video' && videoTrack) {
                sender.replaceTrack(videoTrack).catch(e => { });
            }
        });
    });
}

export function replaceVideoTrackOnAllPeers(newVideoTrack) {
    console.log(" Swapping video track on all peers...");

    peerConnections.forEach((peerInfo) => {
        const pc = peerInfo.pc;

        // Cách 1: Tìm qua Sender (Ưu tiên)
        const senders = pc.getSenders();
        const videoSender = senders.find(s => s.track && s.track.kind === 'video');

        if (videoSender) {
            videoSender.replaceTrack(newVideoTrack)
                .then(() => console.log(`Replaced track for ${peerInfo.username}`))
                .catch(err => console.error(`Replace track error for ${peerInfo.username}:`, err));
        } else {
            // Cách 2: Tìm qua Transceiver (Dự phòng nếu Sender đang rỗng/null)
            const transceivers = pc.getTransceivers();
            const videoTransceiver = transceivers.find(t => t.receiver.track.kind === 'video');
            if (videoTransceiver) {
                videoTransceiver.sender.replaceTrack(newVideoTrack)
                    .catch(err => console.error(`Replace transceiver error for ${peerInfo.username}:`, err));
            }
        }
    });
}

export async function getConnectionStats(connectionId) {
    const peerInfo = peerConnections.get(connectionId);
    if (!peerInfo || !peerInfo.pc) return null;

    try {
        const report = await peerInfo.pc.getStats();
        let stats = {
            rtt: 0,
            packetLoss: 0,
            bitrate: 0, // kbps
            resolution: 'N/A',
            fps: 0
        };

        let inboundRTPVideoStat; // Thống kê nhận video
        let candidatePairStat;   // Thống kê ICE

        report.forEach(stat => {
            if (stat.type === 'inbound-rtp' && stat.kind === 'video') {
                inboundRTPVideoStat = stat;
            } else if (stat.type === 'candidate-pair' && stat.state === 'succeeded') {
                candidatePairStat = stat;
            }
        });

        // 1. Lấy RTT (Độ trễ)
        if (candidatePairStat) {
            stats.rtt = Math.round(candidatePairStat.currentRoundTripTime * 1000);
        }

        // 2. Lấy thông số Video NHẬN ĐƯỢC (Thông tin chính xác nhất về chất lượng)
        if (inboundRTPVideoStat) {
            stats.packetLoss = inboundRTPVideoStat.packetsLost || 0;
            stats.resolution = `${inboundRTPVideoStat.frameWidth || 'N/A'}x${inboundRTPVideoStat.frameHeight || 'N/A'}`;
            stats.fps = inboundRTPVideoStat.framesPerSecond || 0;

            // Tính Bitrate (Tốc độ nhận - Download speed)
            const now = performance.now();
            const bytes = inboundRTPVideoStat.bytesReceived;

            const prev = lastStats.get(connectionId) || {};

            if (prev.timestamp) {
                const duration = (now - prev.timestamp) / 1000; // giây
                const bits = (bytes - prev.bytesReceived) * 8;
                stats.bitrate = Math.round(bits / duration / 1024); // kbps
            }

            // Lưu lại cho lần sau
            lastStats.set(connectionId, { timestamp: now, bytesReceived: bytes });
        }

        return stats;

    } catch (err) {
        console.error("Error getting stats:", err);
        return null;
    }
}

// Các hàm export thừa (giữ lại để tránh lỗi import bên file khác)
export function hasPeerConnection(id) { return peerConnections.has(id); }
export function closeAllPeerConnections() { peerConnections.forEach(p => p.pc.close()); peerConnections.clear(); }

