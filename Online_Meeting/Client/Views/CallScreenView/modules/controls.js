import { LOCAL_ID, AUDIO_CONSTRAINTS_BASE, VIDEO_QUALITY_PRESETS } from './config.js';
import { uiState, meetingContext, screenShareStream, setScreenShareStream, currentVideoQuality } from './state.js';
import { getLocalParticipant, setLocalParticipant, createParticipant, updateParticipantMedia } from './participants.js';
import { buildMediaConstraints, initLocalMedia } from './media.js';
import { renderVideoGrid, updateChatBadge } from './ui.js';
import { postToHost } from './messaging.js';
import { escapeHtml } from './utils.js';
import { updateLocalTracksInPeers, replaceVideoTrackOnAllPeers } from './webrtc.js';
import { createMixedStream, stopMixing } from './videoMixer.js';

// ===== MICROPHONE CONTROL =====

export async function toggleMic() {
    const local = getLocalParticipant();
    if (local && local.stream) {
        const audioTracks = local.stream.getAudioTracks();
        if (audioTracks.length > 0) {
            // Toggle existing tracks
            audioTracks.forEach(t => {
                t.enabled = !t.enabled;
            });
            local.isMuted = !audioTracks.some(t => t.enabled);
            uiState.isMuted = local.isMuted;
            renderVideoGrid();
            postToHost({
                type: 'toggleMic',
                audio: !local.isMuted
            });
        } else {
            // No audio tracks - get microphone access
            await addMicrophoneToStream(local);
        }
    } else {
        // No stream yet - get microphone only
        await createStreamWithMicrophone();
    }
}

async function addMicrophoneToStream(local) {
    try {
        const audioConstraints = { ...AUDIO_CONSTRAINTS_BASE };
        if (meetingContext.micId) {
            audioConstraints.deviceId = { ideal: meetingContext.micId };
        }

        const audioStream = await navigator.mediaDevices.getUserMedia({ audio: audioConstraints });
        const newAudioTrack = audioStream.getAudioTracks()[0];

        // Get existing video tracks
        const videoTracks = local.stream.getVideoTracks();

        // Create new stream with audio + existing video
        const newStream = new MediaStream();
        newStream.addTrack(newAudioTrack);
        videoTracks.forEach(track => newStream.addTrack(track));

        // Update local participant stream
        local.stream = newStream;
        local.isMuted = false;
        uiState.isMuted = false;

        // Update tracks in all peer connections (uses replaceTrack - no renegotiation)
        updateLocalTracksInPeers();

        renderVideoGrid();
        console.log('✅ Added microphone and updated peer connections');
    } catch (err) {
        console.error('Failed to get microphone access:', err);
        alert('Không thể truy cập microphone. Vui lòng kiểm tra quyền truy cập.');
    }
}

async function createStreamWithMicrophone() {
    try {
        const audioConstraints = { ...AUDIO_CONSTRAINTS_BASE };
        if (meetingContext.micId) {
            audioConstraints.deviceId = { ideal: meetingContext.micId };
        }

        const audioStream = await navigator.mediaDevices.getUserMedia({ audio: audioConstraints });
        const localName = getLocalName();

        const newLocal = createParticipant({
            id: LOCAL_ID,
            name: localName,
            avatar: '👤',
            isSpeaking: false,
            isMuted: false,
            isVideoOff: true,
            stream: audioStream
        });

        setLocalParticipant(newLocal);
        uiState.isMuted = false;
        uiState.isVideoOff = true;
        renderVideoGrid();
    } catch (err) {
        console.error('Failed to get microphone access:', err);
        alert('Không thể truy cập microphone. Vui lòng kiểm tra quyền truy cập.');
    }
}

// ===== VIDEO CONTROL =====

export async function toggleVideo() {
    const local = getLocalParticipant();
    if (local && local.stream) {
        const videoTracks = local.stream.getVideoTracks();
        if (videoTracks.length > 0) {
            // Toggle existing tracks
            videoTracks.forEach(t => {
                t.enabled = !t.enabled;
            });
            local.isVideoOff = !videoTracks.some(t => t.enabled);
            uiState.isVideoOff = local.isVideoOff;
            renderVideoGrid();

            postToHost({
                type: 'toggleCamera',
                video: !local.isVideoOff
            });
        } else {
            // No video tracks - get camera access
            await addCameraToStream(local);
        }
    } else {
        // No stream yet - get camera only
        await createStreamWithCamera();
    }
}

async function addCameraToStream(local) {
    try {
        const qualityPreset = VIDEO_QUALITY_PRESETS[currentVideoQuality] || VIDEO_QUALITY_PRESETS.medium;
        const videoConstraints = {
            width: qualityPreset.width,
            height: qualityPreset.height,
            frameRate: qualityPreset.frameRate,
            facingMode: 'user',
            aspectRatio: { ideal: 16 / 9 }
        };
        if (meetingContext.cameraId) {
            videoConstraints.deviceId = { ideal: meetingContext.cameraId };
        }

        const videoStream = await navigator.mediaDevices.getUserMedia({ video: videoConstraints });
        const newVideoTrack = videoStream.getVideoTracks()[0];

        // Get existing audio tracks
        const audioTracks = local.stream.getAudioTracks();

        // Create new stream with existing audio + new video
        const newStream = new MediaStream();
        audioTracks.forEach(track => newStream.addTrack(track));
        newStream.addTrack(newVideoTrack);

        // Update local participant stream
        local.stream = newStream;
        local.isVideoOff = false;
        uiState.isVideoOff = false;

        // Update tracks in all peer connections (uses replaceTrack - no renegotiation)
        updateLocalTracksInPeers();

        renderVideoGrid();
        console.log('✅ Added camera and updated peer connections');
    } catch (err) {
        console.error('Failed to get camera access:', err);
        alert('Không thể truy cập camera. Vui lòng kiểm tra quyền truy cập.');
    }
}

async function createStreamWithCamera() {
    try {
        const qualityPreset = VIDEO_QUALITY_PRESETS[currentVideoQuality] || VIDEO_QUALITY_PRESETS.medium;
        const videoConstraints = {
            width: qualityPreset.width,
            height: qualityPreset.height,
            frameRate: qualityPreset.frameRate,
            facingMode: 'user',
            aspectRatio: { ideal: 16 / 9 }
        };
        if (meetingContext.cameraId) {
            videoConstraints.deviceId = { ideal: meetingContext.cameraId };
        }

        const videoStream = await navigator.mediaDevices.getUserMedia({ video: videoConstraints });
        const localName = getLocalName();

        const newLocal = createParticipant({
            id: LOCAL_ID,
            name: localName,
            avatar: '👤',
            isSpeaking: false,
            isMuted: true,
            isVideoOff: false,
            stream: videoStream
        });

        setLocalParticipant(newLocal);
        uiState.isMuted = true;
        uiState.isVideoOff = false;
        renderVideoGrid();
    } catch (err) {
        console.error('Failed to get camera access:', err);
        alert('Không thể truy cập camera. Vui lòng kiểm tra quyền truy cập.');
    }
}

// ===== SCREEN SHARE CONTROL =====

export async function shareScreen() {
    try {
        if (uiState.isScreenSharing) {
            await stopScreenSharing();
        } else {
            await startScreenSharingMixed();
        }
    } catch (err) {
        console.error('Error sharing screen:', err);
    }
}

async function startScreenSharingMixed() {
    const local = getLocalParticipant();
    if (!local) return;

    try {
        // 1. Lấy Stream Màn hình
        const screenStream = await navigator.mediaDevices.getDisplayMedia({
            video: { cursor: 'always' },
            audio: false
        });

        // 2. Lấy Stream Camera hiện tại
        // (Nếu đang tắt cam thì stream này sẽ đen hoặc không có track, ta cần xử lý kỹ)
        let cameraStream = local.stream;

        // Nếu local stream không hợp lệ (ví dụ người dùng tắt cam từ đầu), thử xin lại quyền video ngầm
        if (!cameraStream || !cameraStream.getActive || cameraStream.getVideoTracks().length === 0 || uiState.isVideoOff) {
            try {
                // Lấy camera nhưng KHÔNG thay đổi trạng thái UI (để dùng cho việc mix thôi)
                const constraints = buildMediaConstraints();
                if (constraints.video) {
                    cameraStream = await navigator.mediaDevices.getUserMedia({ video: constraints.video });
                }
            } catch (e) {
                console.warn("Không lấy được camera để mix, chỉ share màn hình");
                cameraStream = null;
            }
        }

        // 3. TRỘN HÌNH (Mixer)
        let finalStream;
        // Chỉ trộn nếu có camera stream
        if (cameraStream && cameraStream.getVideoTracks().length > 0) {
            finalStream = createMixedStream(screenStream, cameraStream);
        } else {
            finalStream = screenStream; // Chỉ màn hình
        }

        const finalVideoTrack = finalStream.getVideoTracks()[0];
        setScreenShareStream(finalStream);

        // 4. Xử lý nút Stop của trình duyệt
        screenStream.getVideoTracks()[0].onended = () => stopScreenSharing();

        // 5. THAY THẾ TRACK TRÊN ĐƯỜNG TRUYỀN (Quan trọng: Không Renegotiation)
        replaceVideoTrackOnAllPeers(finalVideoTrack);

        // 6. CẬP NHẬT UI & SIGNALR (Để đưa màn hình vào giữa)
        updateParticipantMedia(LOCAL_ID, {
            stream: finalStream, // Local tự nhìn thấy stream đã trộn của mình
            isScreenSharing: true, // Cờ này kích hoạt UI Center Mode
            isVideoOff: false
        });

        uiState.isScreenSharing = true;
        renderVideoGrid(); // Vẽ lại giao diện

        // 7. GỬI TÍN HIỆU CHO NGƯỜI KHÁC
        // Khi người khác nhận tin này, ui.js của họ sẽ set isScreenSharing=true cho bạn 
        // và tự động đưa video của bạn vào trung tâm.
        postToHost({ type: 'ToggleScreenShare', isSharingScreen: true });

        showToast("Screen Share", "Đang chia sẻ màn hình + Camera");

    } catch (err) {
        console.error("User cancelled share", err);
    }
}

export async function stopScreenSharing() {
    if (!uiState.isScreenSharing) return;

    console.log("Stopping screen share...");

    // 1. Dừng bộ trộn
    stopMixing();

    // 2. Dừng track màn hình
    if (screenShareStream) {
        screenShareStream.getTracks().forEach(t => t.stop());
        setScreenShareStream(null);
    }

    // 3. Lấy lại Camera gốc để hiển thị lại mặt mình
    try {
        const constraints = buildMediaConstraints();
        await initLocalMedia(constraints); // Hàm này sẽ reset lại local.stream về Camera

        const local = getLocalParticipant();
        const cameraTrack = local.stream ? local.stream.getVideoTracks()[0] : null;

        // 4. Thay thế lại trên đường truyền
        if (cameraTrack) {
            replaceVideoTrackOnAllPeers(cameraTrack);
        }

        // 5. Reset trạng thái
        uiState.isScreenSharing = false;

        // Tắt cờ -> UI tự động quay về chế độ Lưới (Grid Mode)
        updateParticipantMedia(LOCAL_ID, { isScreenSharing: false });

        renderVideoGrid();

        // 6. Báo Server
        postToHost({ type: 'ToggleScreenShare', isSharingScreen: false });

    } catch (err) {
        console.error("Error reverting to camera:", err);
    }
}

function handleScreenShareError(err) {
    if (err.name === 'NotAllowedError') {
        alert('Quyền chia sẻ màn hình bị từ chối');
    } else if (err.name === 'NotFoundError') {
        alert('Không tìm thấy nguồn màn hình');
    } else {
        alert('Lỗi chia sẻ màn hình: ' + err.message);
    }
}

// ===== CHAT & PARTICIPANTS PANEL =====

export function toggleChat() {
    uiState.isChatOpen = !uiState.isChatOpen;
    const chatPanel = document.getElementById('chatPanel');


    if (uiState.isChatOpen) {
        chatPanel.classList.add('open');
        updateChatBadge(false);

        if (uiState.isParticipantsOpen) {
            uiState.isParticipantsOpen = false;
            document.getElementById('participantsPanel').classList.remove('open');
        }
    } else {
        chatPanel.classList.remove('open');
    }
}

export function toggleParticipants() {
    uiState.isParticipantsOpen = !uiState.isParticipantsOpen;
    const participantsPanel = document.getElementById('participantsPanel');

    if (uiState.isParticipantsOpen) {
        participantsPanel.classList.add('open');
        if (uiState.isChatOpen) {
            uiState.isChatOpen = false;
            document.getElementById('chatPanel').classList.remove('open');
        }
    } else {
        participantsPanel.classList.remove('open');
    }
}

// ===== CHAT MESSAGES =====

export function handleKeyPress(event) {
    if (event.key === 'Enter') {
        sendMessage();
    }
}

// ===== END CALL =====

export function endCall() {
    // 1. Stop WebRTC tracks
    stopScreenSharing(); // Tắt share nếu có
    const local = getLocalParticipant();
    if (local && local.stream) {
        local.stream.getTracks().forEach(t => t.stop());
    }

    // 2. Gửi tín hiệu lên WPF
    postToHost({ type: 'end-call' });

}

// ===== HELPERS =====

function getLocalName() {
    return meetingContext.userName
        ? `${meetingContext.userName} (${meetingContext.isHost ? 'Host' : 'Bạn'})`
        : 'Bạn';
}
