import { meetingContext } from './state.js';
import { LOCAL_ID } from './config.js';
import { addRemoteParticipant, handleParticipantsUpdate, updateParticipantMedia, removeParticipant } from './participants.js';
import { buildMediaConstraints, initLocalMedia } from './media.js';
import { renderVideoGrid, updateRoomUI, renderChatMessage, addGuestToWaitingList, showToast, updateChatBadge } from './ui.js';
import {
    handleParticipantJoined,
    handleParticipantLeft,
    handleOffer,
    handleAnswer,
    handleIceCandidate,
    hasPeerConnection,
    updateLocalTracksInPeers
} from './webrtc.js';

// ===== POST TO HOST =====
export function postToHost(obj) {
    try {
        if (window.chrome && chrome.webview && chrome.webview.postMessage) {
            chrome.webview.postMessage(obj);
        } else if (window.parent) {
            window.parent.postMessage(obj, "*");
        }
    } catch (e) {
        console.warn('Failed to post to host:', e);
    }
}

// ===== RECEIVE FROM HOST =====
export function setupHostMessageListener() {
    if (window.chrome && chrome.webview && chrome.webview.addEventListener) {
        chrome.webview.addEventListener('message', ev => handleHostMessage(ev.data));
    }
    window.addEventListener('message', ev => {
        try { handleHostMessage(ev.data); } catch (err) { console.error(err); }
    });
}

async function handleHostMessage(msg) {
    if (!msg || typeof msg !== 'object') return;
    const { type } = msg;
    console.log('Received message from host:', type, msg);

    switch (type) {
        case 'init-call':
            handleInitCall(msg);
            break;

        case 'participants-update':
            if (msg.participants && Array.isArray(msg.participants)) {
                // 1. Cập nhật danh sách (UI)
                handleParticipantsUpdate(msg.participants);

                // 2. Tạo kết nối với những người chưa kết nối
                msg.participants.forEach(p => {
                    // Bỏ qua bản thân
                    if (p.userId !== meetingContext.userId && p.userId !== LOCAL_ID) {
                        // Chỉ kết nối nếu chưa có
                        if (!hasPeerConnection(p.connectionId)) {
                            console.log(`🔗 Initiating connection to existing peer: ${p.username}`);
                            handleParticipantJoined(
                                p.connectionId,
                                p.userId,
                                p.username,
                                true // ✅ NGƯỜI MỚI: Chủ động gửi Offer cho người cũ
                            );
                        }
                    }
                });

                renderVideoGrid();

                // Ẩn overlay chờ
                const overlay = document.getElementById('waitingOverlay');
                if (overlay) overlay.style.display = 'none';
            }
            break;

        case 'participant-joined':
            if (msg.participant) {
                const p = msg.participant; // ✅ FIX: Khai báo biến p từ msg.participant

                addRemoteParticipant({
                    id: p.id,
                    name: p.name,
                    connectionId: p.connectionId,
                    isVideoOff: !(p.camEnable || false),
                    isMuted: !(p.micEnable || false),
                    stream: null
                });

                console.log(`🔗 Existing user handling joiner: ${p.name}`);

                // Create WebRTC peer connection
                handleParticipantJoined(
                    p.connectionId,
                    p.id,
                    p.name,
                    false // ✅ NGƯỜI CŨ: Chỉ tạo PC, đợi nhận Offer từ người mới
                );

                renderVideoGrid();
            }
            break;

        // ... (Các case khác giữ nguyên không đổi) ...
        case 'unlock-screen':
            const overlay2 = document.getElementById('waitingOverlay');
            if (overlay2) overlay2.style.display = 'none';
            break;
        case 'remote-cam-toggled':
            updateParticipantMedia(msg.id, { isVideoOff: !msg.videoEnabled });
            renderVideoGrid();
            break;
        case 'remote-mic-toggled':
            updateParticipantMedia(msg.id, { isMuted: !msg.audioEnabled });
            renderVideoGrid();
            break;
        case 'remote-screen-sharing-toggled':
            updateParticipantMedia(msg.id, { isScreenSharing: msg.isScreenSharing });
            renderVideoGrid();
            if (msg.isScreenSharing) showToast("SHARE SCREEN", msg.name + " is sharing screen!", 4000);
            break;
        case 'chat-message':
            renderChatMessage(msg);
            if (!uiState.isChatOpen && msg.id !== meetingContext.userId) updateChatBadge(true);
            break;
        case 'you-are-waiting':
            document.getElementById('waitingOverlay').style.display = 'flex';
            break;
        case 'you-are-rejected':
            alert("Người chủ trì đã từ chối yêu cầu tham gia của bạn.");
            postToHost({ type: 'close-window' });
            break;
        case 'guest-requested':
            console.log("JS Received guest request:", msg);
            let guest = { connectionId: msg.connectionId, userId: msg.id, name: msg.name };
            addGuestToWaitingList(guest);
            break;
        case 'participant-left':
            if (msg.id || msg.connectionId) {
                const participantId = msg.id || msg.connectionId;
                removeParticipant(participantId);
                handleParticipantLeft(msg.connectionId);
                renderVideoGrid();
            }
            break;
        case 'receive-offer':
            if (msg.fromConnectionId && msg.offer) {
                await handleOffer(msg.fromConnectionId, msg.fromUserId, msg.fromUsername, msg.offer);
            }
            break;
        case 'receive-answer':
            if (msg.fromConnectionId && msg.answer) {
                await handleAnswer(msg.fromConnectionId, msg.fromUserId, msg.fromUsername, msg.answer);
            }
            break;
        case 'receive-ice-candidate':
            if (msg.fromConnectionId && msg.candidate) {
                await handleIceCandidate(msg.fromConnectionId, msg.candidate);
            }
            break;
        default:
            console.debug('Unhandled host message:', msg);
    }
}

async function handleInitCall(msg) {
    console.log('handleInitCall received:', msg);

    Object.assign(meetingContext, {
        roomId: msg.roomId || null,
        roomName: msg.roomName || 'Cuộc họp',
        roomKey: msg.roomKey || null,
        roomUrl: msg.roomUrl || null,
        userId: msg.userId || LOCAL_ID,
        userName: msg.userName || 'Người dùng',
        isHost: msg.isHost || false,
        cameraId: msg.cameraId || null,
        micId: msg.micId || null,
        audioEnabled: msg.audioEnabled !== undefined ? msg.audioEnabled : true,
        videoEnabled: msg.videoEnabled !== undefined ? msg.videoEnabled : true
    });

    updateRoomUI(meetingContext.roomName, meetingContext.roomKey, meetingContext.roomUrl, meetingContext.isHost);

    const constraints = buildMediaConstraints();
    try {
        await initLocalMedia(constraints);

        if (msg.participants && Array.isArray(msg.participants)) {
            msg.participants.forEach(p => {
                if (p.userId !== meetingContext.userId && p.userId !== LOCAL_ID) {
                    addRemoteParticipant({
                        id: p.userId,
                        name: p.username,
                        avatar: '👤',
                        isMuted: !p.micEnable, // Đảo ngược logic
                        isVideoOff: !p.camEnable, // Đảo ngược logic
                        stream: null
                    });

                    // Init call: Đây là danh sách người cũ, mình là người mới
                    // => Mình sẽ chủ động gửi Offer
                    handleParticipantJoined(
                        p.connectionId,
                        p.userId,
                        p.username,
                        true // ✅ FIX: Init call thì mình là người mới -> True
                    );
                }
            });
        }

        const waitingPanel = document.getElementById('waitingRoomArea');
        if (waitingPanel) {
            waitingPanel.style.display = meetingContext.isHost ? 'block' : 'none';
        }

        renderVideoGrid();

        setTimeout(() => {
            console.log("Triggering post-init track update...");
            updateLocalTracksInPeers();
        }, 1000); // Delay nhẹ để đảm bảo mọi thứ ổn định

        const overlay = document.getElementById('waitingOverlay');
        if (msg.isWaiting) {
            if (overlay) overlay.style.display = 'flex';
        } else {
            if (overlay) overlay.style.display = 'none';
        }
    } catch (err) {
        console.error('Failed to initialize local media:', err);
    }
}