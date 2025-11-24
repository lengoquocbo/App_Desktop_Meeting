import { LOCAL_ID, DEFAULT_VIDEO_QUALITY } from './config.js';

// ===== UI STATE =====
export const uiState = {
    isMuted: false,
    isVideoOff: false,
    isChatOpen: false,
    isParticipantsOpen: false,
    isScreenSharing: false,
    isSharingCamera: false
};

// ===== MEETING CONTEXT =====
export const meetingContext = {
    roomId: null,
    roomName: null,
    roomKey: null,
    roomUrl: null,
    userId: null,
    userName: null,
    isHost: false,
    cameraId: null,
    micId: null,
    audioEnabled: true,
    videoEnabled: true
};

// ===== VIDEO QUALITY =====
export let currentVideoQuality = DEFAULT_VIDEO_QUALITY;

export function setVideoQuality(quality) {
    currentVideoQuality = quality;
}

// ===== SCREEN SHARE STREAM =====
export let screenShareStream = null;

export function setScreenShareStream(stream) {
    screenShareStream = stream;
}

// ===== INITIAL SETTINGS =====
export let initialSettings = null;

export function setInitialSettings(settings) {
    initialSettings = settings;
}
