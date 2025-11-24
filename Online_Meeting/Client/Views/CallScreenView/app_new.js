// app.js - Main entry point
// ==========================
// Refactored modular architecture for Online Meeting application

// ===== IMPORTS =====
import { VIDEO_QUALITY_PRESETS } from './modules/config.js';
import { currentVideoQuality } from './modules/state.js';
import {
    addRemoteParticipant,
    removeParticipant,
    updateParticipantMedia
} from './modules/participants.js';
import { initLocalMedia, changeVideoQuality } from './modules/media.js';
import { renderVideoGrid, updateClock, sendMessage, handleAdmit, handleReject, toggleStats } from './modules/ui.js';
import {
    toggleMic,
    toggleVideo,
    shareScreen,
    toggleChat,
    toggleParticipants,
    handleKeyPress,
    endCall
} from './modules/controls.js';
import { setupHostMessageListener } from './modules/messaging.js';

// ===== EXPOSE FUNCTIONS GLOBALLY (for HTML onclick handlers) =====
window.toggleMic = toggleMic;
window.toggleVideo = toggleVideo;
window.shareScreen = shareScreen;
window.toggleChat = toggleChat;
window.toggleParticipants = toggleParticipants;
window.sendMessage = sendMessage;
window.handleAdmit = handleAdmit;
window.handleReject = handleReject;
window.toggleStats = toggleStats;
window.handleKeyPress = handleKeyPress;
window.endCall = endCall;

// ===== EXPOSE API FOR C# HOST & DEBUGGING =====
window.appMeeting = {
    // Participant management
    addRemoteParticipant,
    removeParticipant,
    updateParticipantMedia,

    // Rendering
    renderVideoGrid,

    // Media
    initLocalMedia,
    changeVideoQuality,

    // Quality presets
    getVideoQualityPresets: () => Object.keys(VIDEO_QUALITY_PRESETS),
    getCurrentVideoQuality: () => currentVideoQuality
};

// ===== INITIALIZATION =====
document.addEventListener('DOMContentLoaded', async () => {
    console.log('=== Online Meeting Application Starting ===');

    // Setup host message listener (WebView2 communication)
    setupHostMessageListener();

    // Start real-time clock
    updateClock();
    setInterval(updateClock, 1000);

    // Initial render (empty until init-call received from C# host)
    renderVideoGrid();

    console.log('Waiting for init-call message from host...');
    console.log('appMeeting API exposed:', window.appMeeting);
});

console.log('app.js loaded - Modular architecture initialized');
