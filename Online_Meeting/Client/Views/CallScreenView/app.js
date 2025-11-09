// ===== STATE VARIABLES =====
let isMuted = false;
let isVideoOff = false;
let isChatOpen = false;
let isParticipantsOpen = false;

// ===== PARTICIPANTS DATA =====
const allParticipants = [
    { id: 1, name: 'Huy Nguyen (Host, me)', avatar: '👨‍💼', isSpeaking: true, isMuted: false, isVideoOff: false },
    { id: 2, name: 'Nguyễn Văn A', avatar: '👨‍💻', isSpeaking: false, isMuted: true, isVideoOff: false },
    { id: 3, name: 'Trần Thị B', avatar: '👩‍💼', isSpeaking: false, isMuted: false, isVideoOff: false },
    { id: 4, name: 'Lê Văn C', avatar: '👨‍🎓', isSpeaking: false, isMuted: false, isVideoOff: true },
    { id: 5, name: 'Phạm Thị D', avatar: '👩‍💻', isSpeaking: false, isMuted: false, isVideoOff: false },
    { id: 6, name: 'Hoàng Văn E', avatar: '👨‍🔬', isSpeaking: false, isMuted: true, isVideoOff: false },
    { id: 7, name: 'Đỗ Thị F', avatar: '👩‍🎓', isSpeaking: false, isMuted: false, isVideoOff: false }
];

let currentParticipants = [allParticipants[0]];
const MAX_VISIBLE = 5;

// ===== RENDER FUNCTIONS =====
function renderVideoGrid() {
    const videoGrid = document.getElementById('videoGrid');
    const totalCount = currentParticipants.length;

    let visibleCount = totalCount;
    let showMore = false;

    if (totalCount > MAX_VISIBLE) {
        visibleCount = MAX_VISIBLE - 1;
        showMore = true;
    }

    // Update grid class
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

    // Render videos
    let html = '';

    for (let i = 0; i < visibleCount; i++) {
        const p = currentParticipants[i];
        html += `
            <div class="video-tile ${p.isSpeaking ? 'speaking' : ''}">
                <div class="avatar">${p.avatar}</div>
                <div class="participant-name">${p.name}</div>
                ${p.isMuted ? '<div class="mic-status"><i class="bi bi-mic-mute-fill"></i></div>' : ''}
            </div>
        `;
    }

    if (showMore) {
        const hiddenCount = totalCount - visibleCount;
        html += `
            <div class="video-tile more-tile" onclick="showAllParticipants()">
                <div class="more-count">+${hiddenCount}</div>
                <div class="more-text">Xem thêm</div>
            </div>
        `;
    }

    videoGrid.innerHTML = html;

    document.getElementById('participantCount').textContent = totalCount;
    document.getElementById('participantCountPanel').textContent = totalCount;

    renderParticipantList();
}

function renderParticipantList() {
    const participantList = document.getElementById('participantList');
    let html = '';

    currentParticipants.forEach(p => {
        html += `
            <div class="participant-item">
                <div class="participant-avatar">${p.avatar}</div>
                <div class="participant-info">
                    <div class="participant-info-name">${p.name}</div>
                    <div class="participant-info-status">${p.isSpeaking ? 'Đang nói' : 'Im lặng'}</div>
                </div>
                <div class="participant-controls">
                    <div class="participant-icon ${p.isMuted ? 'muted' : 'active'}">
                        <i class="bi bi-${p.isMuted ? 'mic-mute-fill' : 'mic-fill'}"></i>
                    </div>
                    <div class="participant-icon ${p.isVideoOff ? 'muted' : 'active'}">
                        <i class="bi bi-${p.isVideoOff ? 'camera-video-off-fill' : 'camera-video-fill'}"></i>
                    </div>
                </div>
            </div>
        `;
    });

    participantList.innerHTML = html;
}

// ===== CONTROL FUNCTIONS =====
function toggleMic() {
    isMuted = !isMuted;
    const btn = document.getElementById('micBtn');
    const icon = document.getElementById('micIcon');

    if (isMuted) {
        btn.classList.add('active');
        icon.className = 'bi bi-mic-mute-fill';
    } else {
        btn.classList.remove('active');
        icon.className = 'bi bi-mic-fill';
    }
}

function toggleVideo() {
    isVideoOff = !isVideoOff;
    const btn = document.getElementById('videoBtn');
    const icon = document.getElementById('videoIcon');

    if (isVideoOff) {
        btn.classList.add('active');
        icon.className = 'bi bi-camera-video-off-fill';
    } else {
        btn.classList.remove('active');
        icon.className = 'bi bi-camera-video-fill';
    }
}

function toggleChat() {
    isChatOpen = !isChatOpen;
    const chatPanel = document.getElementById('chatPanel');

    if (isChatOpen) {
        chatPanel.classList.add('open');
        if (isParticipantsOpen) {
            isParticipantsOpen = false;
            document.getElementById('participantsPanel').classList.remove('open');
        }
    } else {
        chatPanel.classList.remove('open');
    }
}

function toggleParticipants() {
    isParticipantsOpen = !isParticipantsOpen;
    const participantsPanel = document.getElementById('participantsPanel');

    if (isParticipantsOpen) {
        participantsPanel.classList.add('open');
        if (isChatOpen) {
            isChatOpen = false;
            document.getElementById('chatPanel').classList.remove('open');
        }
    } else {
        participantsPanel.classList.remove('open');
    }
}

function addParticipant() {
    if (currentParticipants.length < allParticipants.length) {
        currentParticipants.push(allParticipants[currentParticipants.length]);
    } else {
        if (currentParticipants.length > 1) {
            currentParticipants.pop();
        }
    }
    renderVideoGrid();
}

function showAllParticipants() {
    const participantNames = currentParticipants.map((p, i) => `${i + 1}. ${p.name}`).join('\n');
    alert(`Danh sách tất cả ${currentParticipants.length} người tham gia:\n\n${participantNames}`);
}

function sendMessage() {
    const input = document.getElementById('messageInput');
    const message = input.value.trim();

    if (message) {
        const messagesDiv = document.getElementById('chatMessages');
        const now = new Date();
        const time = now.getHours().toString().padStart(2, '0') + ':' + now.getMinutes().toString().padStart(2, '0');

        const messageHTML = `
            <div class="message">
                <div class="message-user">Bạn</div>
                <div class="message-text">${escapeHtml(message)}</div>
                <div class="message-time">${time}</div>
            </div>
        `;

        messagesDiv.innerHTML += messageHTML;
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
        input.value = '';
    }
}

function handleKeyPress(event) {
    if (event.key === 'Enter') {
        sendMessage();
    }
}

function endCall() {
    if (confirm('Bạn có chắc muốn kết thúc cuộc họp?')) {
        alert('Cuộc họp đã kết thúc. Cảm ơn bạn đã tham gia! 👋');
    }
}

// ===== UTILITY FUNCTIONS =====
function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

// ===== INITIALIZE =====
document.addEventListener('DOMContentLoaded', function () {
    renderVideoGrid();
});

// Initialize on load
renderVideoGrid();