import { LOCAL_ID, MAX_VISIBLE_PARTICIPANTS } from './config.js';
import { getCurrentParticipantsArray, participantsById } from './participants.js';
import { meetingContext, uiState } from './state.js';
import { postToHost } from './messaging.js';
import { escapeHtml } from './utils.js';
import { getConnectionStats } from './webrtc.js';


//================STAT PANEL======================
export function toggleStats() {
    const panel = document.getElementById('statsPanel');
    if (!panel) return;

    const isClosed = !panel.classList.contains('open');

    // Đóng các panel khác trước
    document.querySelectorAll('.side-panel').forEach(p => p.classList.remove('open'));

    // Reset state các nút khác (chat/participants) về false nếu cần
    uiState.isChatOpen = false;
    uiState.isParticipantsOpen = false;

    if (isClosed) {
        panel.classList.add('open');
    }
}

export function updateStatsUI(userId, stats) {
    const container = document.getElementById('statsList');
    if (!container) return;

    console.log("[DEBUG] Stat is called")
    // Tìm item của user này trong panel
    let item = document.getElementById(`stats-item-${userId}`);

    // Lấy tên user
    const p = participantsById.get(userId);
    const userName = p ? p.name : 'Unknown';

    // Nếu chưa có thì tạo mới
    if (!item) {
        item = document.createElement('div');
        item.id = `stats-item-${userId}`;
        item.className = 'stats-item';
        item.innerHTML = `<div class="stats-name">${userName}</div><div class="stats-body"></div>`;
        container.appendChild(item);
    }

    // Đánh giá chất lượng mạng để đổi màu
    let qualityClass = 'text-good';
    if (stats.packetLoss > 5 || stats.rtt > 200) qualityClass = 'text-danger';
    else if (stats.packetLoss > 1 || stats.rtt > 100) qualityClass = 'text-warning';

    // Cập nhật nội dung
    const body = item.querySelector('.stats-body');
    body.innerHTML = `
        <div class="stats-row">
            <span>Độ phân giải:</span> 
            <span class="stats-value">${stats.resolution}</span>
        </div>
        <div class="stats-row">
            <span>Tốc độ (Bitrate):</span> 
            <span class="stats-value">${stats.bitrate} kbps</span>
        </div>
        <div class="stats-row">
            <span>FPS:</span> 
            <span class="stats-value">${stats.fps}</span>
        </div>
        <div class="stats-row">
            <span>Mất gói (Packet Loss):</span> 
            <span class="stats-value ${qualityClass}">${stats.packetLoss}%</span>
        </div>
        <div class="stats-row">
            <span>Độ trễ (RTT):</span> 
            <span class="stats-value ${qualityClass}">${stats.rtt} ms</span>
        </div>
    `;
}
export function removeStatsUI(userId) {
    const item = document.getElementById(`stats-item-${userId}`);
    if (item) item.remove();
}

setInterval(async () => {
    const participants = getCurrentParticipantsArray();

    for (const p of participants) {
        console.log("[debug] local id " + LOCAL_ID);
        if (p.id === LOCAL_ID) continue;

        // Bổ sung kiểm tra connectionId trước khi gọi hàm WebRTC
        if (!p.connectionId) {
            removeStatsUI(p.id); // Loại bỏ nếu nó từng tồn tại
            continue;
        }

        const stats = await getConnectionStats(p.connectionId);
        console.log("[DEBUG] connection id of p is " + p.connectionId);

        if (stats) {
            // THÀNH CÔNG: Cập nhật UI
            updateStatsUI(p.id, stats);
        } else {
            // THẤT BẠI: Nếu không lấy được stats (PC chưa sẵn sàng/đã ngắt), xóa khỏi UI
            removeStatsUI(p.id);
        }
    }
}, 2000);

//============SEND NOTIFICATION================
export function showToast(title, message, duration = 5000) {
    // Tạo container nếu chưa có
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    // Tạo element thông báo
    const toast = document.createElement('div');
    toast.className = 'toast-notification';

    // Âm thanh thông báo nhẹ (Tùy chọn)
    // playNotificationSound(); 

    toast.innerHTML = `
        <div class="toast-icon">
            <i class="bi bi-person-plus-fill"></i>
        </div>
        <div class="toast-content">
            <div class="toast-title">${escapeHtml(title)}</div>
            <div class="toast-message">${escapeHtml(message)}</div>
        </div>
        <div class="toast-close" style="cursor:pointer; color:#9ca3af;">
            <i class="bi bi-x"></i>
        </div>
    `;

    // Xử lý đóng
    const closeBtn = toast.querySelector('.toast-close');
    closeBtn.onclick = () => removeToast(toast);

    // Tự động đóng sau duration
    setTimeout(() => {
        removeToast(toast);
    }, duration);

    container.appendChild(toast);
}

export function updateWaitingBadge(count) {
    const badge = document.getElementById('waitingNotificationDot');

    // Nếu không phải Host (badge có thể không tồn tại hoặc nên ẩn), ta bỏ qua
    // Tuy nhiên logic JS chỉ gọi hàm này nếu là Host, nên cứ check null cho an toàn
    if (!badge) return;

    if (count > 0) {
        badge.style.display = 'block'; // Hiện
    } else {
        badge.style.display = 'none';  // Ẩn
    }
}

export function updateChatBadge(show) {
    const badge = document.getElementById('chatNotificationDot');
    if (badge) {
        badge.style.display = show ? 'block' : 'none';
    }
}


function removeToast(toast) {
    if (!toast) return;
    toast.classList.add('hiding');
    toast.addEventListener('animationend', () => {
        if (toast.parentElement) {
            toast.remove();
        }
    });
}


//=======SEND MESSAGE==============
export function sendMessage() {
    const input = document.getElementById('messageInput');
    const content = input.value.trim();

    if (content) {
        // Gửi lên WPF
        postToHost({
            type: 'send-chat',
            content: content
        });

        input.value = ''; // Xóa ô nhập
        input.focus();
    }
}
export function renderChatMessage(msg) {
    const messagesDiv = document.getElementById('chatMessages');

    // Kiểm tra xem tin nhắn này có phải của mình không
    // So sánh ID server gửi về với ID của mình trong meetingContext
    const myId = String(meetingContext.userId).toLowerCase();
    const msgId = String(msg.id).toLowerCase();

    console.log(`🔍 Chat Check: MsgID[${msgId}] vs MyID[${myId}]`);

    const isMe = msgId === myId;

    const messageDiv = document.createElement('div');
    // Class CSS khác nhau để căn trái/phải
    messageDiv.className = `message ${isMe ? 'my-message' : 'other-message'}`;

    messageDiv.innerHTML = `
        <div class="message-header">
            <span class="user">${isMe ? 'Bạn' : msg.username}</span>
            <span class="time">${msg.timestamp}</span>
        </div>
        <div class="message-content">${escapeHtml(msg.content)}</div>
    `;

    messagesDiv.appendChild(messageDiv);

    if (!isMe) {
        updateChatBadge(true);
        showToast("CHAT MESSAGE", msg.content, 4000);
    }

    // Tự động cuộn xuống dưới cùng
    messagesDiv.scrollTop = messagesDiv.scrollHeight;
}

//==========WAITING GUEST=============
// Danh sách người đang chờ (Local state)
const waitingGuests = new Map();

export function addGuestToWaitingList(guest) {
    if (waitingGuests.has(guest.connectionId)) return;

    waitingGuests.set(guest.connectionId, guest);
    renderWaitingList();

    // Hiện chấm đỏ thông báo
    updateWaitingBadge(waitingGuests.size);

    // (Tùy chọn) Hiện Toast thông báo góc màn hình
    showToast('Yêu cầu tham gia', `${guest.name} muốn vào cuộc họp.`); }

export function renderWaitingList() {
    const listEl = document.getElementById('waitingList');
    const countEl = document.getElementById('waitingCount');

    if (!listEl) return;

    listEl.innerHTML = '';
    countEl.innerText = waitingGuests.size;

    waitingGuests.forEach(guest => {
        const item = document.createElement('div');
        item.className = 'participant-item waiting-item';
        item.innerHTML = `
            <div class="participant-avatar">
                ${guest.name ? guest.name.charAt(0).toUpperCase() : '?'}
            </div>
            <div class="participant-info">
                <div class="participant-info-name">${escapeHtml(guest.name || 'Unknown')}</div>
                <div class="participant-info-status" style="font-size: 11px; color: #6b7280;">Đang chờ duyệt...</div>
            </div>
            <div class="participant-controls">
                <button class="btn-accept" title="Chấp nhận" onclick="handleAdmit('${guest.connectionId}')">
                    <i class="bi bi-check-lg"></i>
                </button>
                <button class="btn-deny" title="Từ chối" onclick="handleReject('${guest.connectionId}')">
                    <i class="bi bi-x-lg"></i>
                </button>
            </div>
        `;
        listEl.appendChild(item);
    });
}

// Hàm gọi từ UI
export async function handleAdmit(connId){
    postToHost({ type: 'admit-guest', toConnectionId: connId });
    waitingGuests.delete(connId); // Xóa khỏi UI ngay cho mượt
    renderWaitingList();
    updateWaitingBadge(waitingGuests.size);
};

export async function handleReject(connId){
    postToHost({ type: 'reject-guest', toConnectionId: connId });
    waitingGuests.delete(connId);
    renderWaitingList();
    updateWaitingBadge(waitingGuests.size);
};

export function toggleWaitingPanel() {
    const panel = document.getElementById('waitingPanel');
    const isClosed = !panel.classList.contains('open');

    // Đóng các panel khác
    document.querySelectorAll('.side-panel').forEach(p => p.classList.remove('open'));

    if (isClosed) panel.classList.add('open');
}


// ===== VIDEO GRID RENDERING =====
/**
 * Render video grid with all participants
 */
export function renderVideoGrid() {
    const videoGrid = document.getElementById('videoGrid');
    const participants = getCurrentParticipantsArray();

    // 1. Kiểm tra xem có ai đang share screen không
    // (Tìm người có cờ isScreenSharing hoặc có luồng màn hình)
    const presenter = participants.find(p =>
        p.isScreenSharing ||
        (p.stream && p.stream.getVideoTracks()[0]?.label.toLowerCase().includes('screen'))
    );

    videoGrid.innerHTML = ''; // Xóa cũ

    if (presenter) {
        // --- CHẾ ĐỘ THUYẾT TRÌNH (Presentation Mode) ---
        renderPresentationMode(videoGrid, presenter, participants);
    } else {
        // --- CHẾ ĐỘ LƯỚI (Standard Grid Mode) ---
        renderStandardGridMode(videoGrid, participants);
    }

    updateParticipantCounts(participants.length);

    renderParticipantList();
}

function renderPresentationMode(container, presenter, allParticipants) {
    container.className = 'video-grid presentation-mode';

    // A. Tạo Main Stage (Màn hình share)
    const mainStage = document.createElement('div');
    mainStage.className = 'main-stage';

    // Tạo video element cho màn hình share
    const screenVideo = document.createElement('video');
    screenVideo.autoplay = true;
    screenVideo.playsInline = true;
    screenVideo.muted = true; // Màn hình thường không có tiếng hoặc đã mix audio riêng
    screenVideo.srcObject = presenter.stream; // Stream chính lúc này là màn hình

    mainStage.appendChild(screenVideo);

    // Thêm nhãn tên người share
    const label = document.createElement('div');
    label.className = 'participant-name';
    label.textContent = `${presenter.name} đang trình bày`;
    label.style.position = 'absolute';
    label.style.bottom = '10px';
    label.style.left = '10px';
    mainStage.appendChild(label);

    container.appendChild(mainStage);

    // B. Tạo Sidebar (Camera của mọi người)
    const sidebar = document.createElement('div');
    sidebar.className = 'sidebar-strip';

    // Lọc danh sách hiển thị trong sidebar
    // Hiển thị tất cả mọi người (bao gồm cả Camera phụ của người đang share nếu có)
    allParticipants.forEach(p => {
        // Nếu là người đang share, ta cần hiển thị CAMERA của họ (secondaryStream)
        // Nếu là người khác, hiển thị stream chính (camera) của họ

        const sidebarTile = document.createElement('div');
        sidebarTile.className = 'video-tile';

        if (p.id === presenter.id) {
            // Với người present: Hiển thị Camera (secondaryStream) nếu có
            if (p.secondaryStream) {
                renderVideoElement(sidebarTile, { ...p, stream: p.secondaryStream }, true); // true = force cover
            } else {
                // Nếu không có secondaryStream, hiển thị Avatar
                renderAvatarElement(sidebarTile, p);
            }
        } else {
            // Với người xem: Hiển thị bình thường
            renderTileContent(sidebarTile, p);
        }

        // Thêm tên nhỏ
        const nameTag = document.createElement('div');
        nameTag.className = 'participant-name';
        nameTag.style.fontSize = '12px';
        nameTag.textContent = p.id === LOCAL_ID ? 'Bạn' : p.name;
        sidebarTile.appendChild(nameTag);

        sidebar.appendChild(sidebarTile);
    });

    container.appendChild(sidebar);
}

function renderStandardGridMode(container, participants) {
    updateVideoGridClass(container, participants.length); // Hàm cũ để set class count-1, count-2...

    let visibleCount = participants.length;
    // Logic ẩn bớt nếu quá đông (giữ nguyên logic cũ của bạn)
    if (participants.length > MAX_VISIBLE_PARTICIPANTS) {
        visibleCount = MAX_VISIBLE_PARTICIPANTS - 1;
    }

    for (let i = 0; i < visibleCount; i++) {
        const p = participants[i];
        const tile = document.createElement('div');
        tile.className = `video-tile ${p.isSpeaking ? 'speaking' : ''}`;

        renderTileContent(tile, p);

        // Các label tên, mic...
        addTileOverlays(tile, p);

        container.appendChild(tile);
    }

    // Render nút "+Xem thêm" nếu cần
    if (participants.length > visibleCount) {
        renderMoreTile(container, participants.length - visibleCount);
    }
}

/**
 * Helper: Quyết định render Video hay Avatar cho 1 ô
 */
function renderTileContent(container, p) {
    // Logic hiển thị video
    if (p.stream && !p.isVideoOff) {
        renderVideoElement(container, p);
    } else {
        renderAvatarElement(container, p);
    }
}


function renderVideoElement(container, p, forceCover = false) {
    const videoWrap = document.createElement('div');
    videoWrap.className = 'video-wrap';

    const video = document.createElement('video');
    video.autoplay = true;
    video.playsInline = true;
    video.muted = p.id === LOCAL_ID;
    video.srcObject = p.stream;

    const track = p.stream?.getVideoTracks()[0];
    const isTrackScreen = track && (track.label.toLowerCase().includes('screen') || track.label.toLowerCase().includes('display'));
    const isScreen = !forceCover && (p.isScreenSharing || isTrackScreen);

    const isLocal = p.id === LOCAL_ID;

    // 2. Chỉ lật ngược khi: Là Local VÀ KHÔNG PHẢI là màn hình
    if (isLocal && !isScreen) {
        video.classList.add('mirrored');
    } else {
        video.classList.remove('mirrored');
    }

    // 3. CSS Object Fit (Màn hình thì contain, Cam thì cover)
    video.style.objectFit = isScreen ? 'contain' : 'cover';
    video.style.backgroundColor = isScreen ? '#000' : '#202124';

    videoWrap.appendChild(video);
    container.appendChild(videoWrap);
}


function addTileOverlays(container, p) {
    // Add name label
    const nameEl = document.createElement('div');
    nameEl.className = 'participant-name';
    nameEl.textContent = p.name;
    container.appendChild(nameEl);

    // Add mic status if muted
    if (p.isMuted) {
        const micEl = document.createElement('div');
        micEl.className = 'mic-status';
        micEl.innerHTML = '<i class="bi bi-mic-mute-fill"></i>';
        container.appendChild(micEl);
    }
}

/**
 * Render participant list in side panel
 */
export function renderParticipantList() {
    const participantList = document.getElementById('participantList');
    if (!participantList) return;

    participantList.innerHTML = '';

    const participants = getCurrentParticipantsArray();
    console.log('renderParticipantList - Total participants:', participants.length);

    participants.forEach(p => {
        const participantItem = createParticipantListItem(p);
        participantList.appendChild(participantItem);
    });
}

// ===== HELPER FUNCTIONS =====

function updateVideoGridClass(videoGrid, totalCount) {
    videoGrid.className = 'video-grid';
    if (totalCount === 1) {
        videoGrid.classList.add('count-1');
    } else if (totalCount === 2) {
        videoGrid.classList.add('count-2');
    } else if (totalCount <= 4) {
        videoGrid.classList.add('count-4');
    } else {
        videoGrid.classList.add('count-more');
    }
}

function renderAvatarElement(container, p) {
    const avatar = document.createElement('div');
    avatar.className = 'avatar';
    avatar.textContent = p.avatar;
    container.appendChild(avatar);
}

function renderMoreTile(videoGrid, hiddenCount) {
    const moreContainer = document.createElement('div');
    moreContainer.className = 'video-tile more-tile';
    moreContainer.onclick = showAllParticipants;

    const moreCount = document.createElement('div');
    moreCount.className = 'more-count';
    moreCount.textContent = `+${hiddenCount}`;
    moreContainer.appendChild(moreCount);

    const moreText = document.createElement('div');
    moreText.className = 'more-text';
    moreText.textContent = 'Xem thêm';
    moreContainer.appendChild(moreText);

    videoGrid.appendChild(moreContainer);
}


function updateParticipantCounts(count) {
    const participantCountEl = document.getElementById('participantCount');
    const participantCountPanelEl = document.getElementById('participantCountPanel');
    if (participantCountEl) participantCountEl.textContent = count;
    if (participantCountPanelEl) participantCountPanelEl.textContent = count;
}

function createParticipantListItem(p) {
    console.log(`Rendering participant ${p.name}:`, {
        id: p.id,
        hasStream: !!p.stream,
        isVideoOff: p.isVideoOff,
        isMuted: p.isMuted
    });

    const participantItem = document.createElement('div');
    participantItem.className = 'participant-item';
    participantItem.setAttribute('data-id', p.id);

    // Avatar/Video container
    const avatarContainer = document.createElement('div');
    avatarContainer.className = 'participant-avatar';

    if (p.stream && !p.isVideoOff) {
        console.log(`Showing video for ${p.name}`);
        const video = document.createElement('video');
        video.autoplay = true;
        video.playsInline = true;
        video.muted = true;
        video.srcObject = p.stream;
        video.style.width = '100%';
        video.style.height = '100%';
        video.style.objectFit = 'cover';
        video.style.borderRadius = '8px';
        avatarContainer.appendChild(video);
    } else {
        console.log(`Showing avatar for ${p.name}`);
        avatarContainer.textContent = p.avatar;
    }

    // Participant info
    const participantInfo = document.createElement('div');
    participantInfo.className = 'participant-info';
    participantInfo.innerHTML = `
        <div class="participant-info-name">${p.name}</div>
        <div class="participant-info-status">${p.isSpeaking ? 'Đang nói' : 'Im lặng'}</div>
    `;

    // Controls
    const participantControls = document.createElement('div');
    participantControls.className = 'participant-controls';
    participantControls.innerHTML = `
        <div class="participant-icon ${p.isMuted ? 'muted' : 'active'}">
            <i class="bi bi-${p.isMuted ? 'mic-mute-fill' : 'mic-fill'}"></i>
        </div>
        <div class="participant-icon ${p.isVideoOff ? 'muted' : 'active'}">
            <i class="bi bi-${p.isVideoOff ? 'camera-video-off-fill' : 'camera-video-fill'}"></i>
        </div>
    `;

    participantItem.appendChild(avatarContainer);
    participantItem.appendChild(participantInfo);
    participantItem.appendChild(participantControls);

    return participantItem;
}

function showAllParticipants() {
    const participants = getCurrentParticipantsArray();
    const names = participants.map((p, i) => `${i + 1}. ${p.name}`).join('\n');
    alert(`Danh sách tất cả ${participants.length} người tham gia:\n\n${names}`);
}

// ===== ROOM INFO UI =====

/**
 * Update room information in UI
 */
export function updateRoomUI(roomName, roomKey, roomUrl, isHost) {
    // Update room name in header
    const roomNameEl = document.getElementById('roomName');
    if (roomNameEl) {
        const hostBadge = isHost ? ' <span class="host-badge">Host</span>' : '';
        roomNameEl.innerHTML = `${roomName}${hostBadge}`;
    }

    // Update document title
    document.title = `${roomName} - Online Meeting`;

    // Display room key and URL
    displayRoomInfo(roomKey, roomUrl);
}

function displayRoomInfo(roomKey, roomUrl) {
    const meetingInfoEl = document.querySelector('.meeting-info');
    if (!meetingInfoEl) return;

    // Remove existing room info
    const existingInfo = meetingInfoEl.querySelector('.room-info-badge');
    if (existingInfo) existingInfo.remove();

    // Create room info badges
    if (roomKey || roomUrl) {
        if (roomKey) {
            const roomKeyBadge = createRoomInfoBadge(
                'room-key',
                `<i class="bi bi-key-fill"></i> ${roomKey}`,
                'Click để copy room Key',
                () => copyToClipboard(roomKey, 'Đã copy room key!')
            );
            meetingInfoEl.appendChild(roomKeyBadge);
        }

        if (roomUrl) {
            const roomUrlBadge = createRoomInfoBadge(
                'room-url',
                `<i class="bi bi-link-45deg"></i> ${roomUrl}`,
                'Click để copy room URL',
                () => copyToClipboard(roomUrl, 'Đã copy room URL!')
            );
            meetingInfoEl.appendChild(roomUrlBadge);
        }
    }
}

function createRoomInfoBadge(className, innerHTML, title, clickHandler) {
    const badge = document.createElement('div');
    badge.className = 'room-info-badge';
    badge.title = title;
    badge.style.cursor = 'pointer';
    badge.innerHTML = `<div class="${className}">${innerHTML}</div>`;
    badge.addEventListener('click', clickHandler);
    return badge;
}

function copyToClipboard(text, message) {
    navigator.clipboard.writeText(text).then(() => {
        alert(message);
    });
}

// ===== CLOCK =====

/**
 * Update real-time clock
 */
export function updateClock() {
    const clockEl = document.querySelector('.meeting-time span:last-child');
    if (clockEl) {
        const now = new Date();
        const hours = now.getHours().toString().padStart(2, '0');
        const minutes = now.getMinutes().toString().padStart(2, '0');
        const seconds = now.getSeconds().toString().padStart(2, '0');
        clockEl.textContent = `${hours}:${minutes}:${seconds}`;
    }
}
