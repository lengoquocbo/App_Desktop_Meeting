// ===== CONSTANTS =====
export const LOCAL_ID = 'local';
export const MAX_VISIBLE_PARTICIPANTS = 5;
export const VirtualHost = 'appassets.example';

// ===== VIDEO QUALITY PRESETS =====
export const VIDEO_QUALITY_PRESETS = {
    low: {
        width: { ideal: 640 },
        height: { ideal: 480 },
        frameRate: { ideal: 15 }
    },
    medium: {
        width: { ideal: 1280 },
        height: { ideal: 720 },
        frameRate: { ideal: 24 }
    },
    high: {
        width: { ideal: 1920 },
        height: { ideal: 1080 },
        frameRate: { ideal: 30 }
    },
    ultra: {
        width: { ideal: 3840 },
        height: { ideal: 2160 },
        frameRate: { ideal: 60 }
    }
};

// ===== AUDIO CONSTRAINTS =====
export const AUDIO_CONSTRAINTS_BASE = {
    echoCancellation: true,
    noiseSuppression: true,
    autoGainControl: true,
    sampleRate: { ideal: 48000 },
    channelCount: { ideal: 1 }
};

// ===== DEFAULT SETTINGS =====
export const DEFAULT_VIDEO_QUALITY = 'high';
