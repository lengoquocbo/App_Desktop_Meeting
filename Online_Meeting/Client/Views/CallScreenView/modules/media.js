import { LOCAL_ID, VIDEO_QUALITY_PRESETS, AUDIO_CONSTRAINTS_BASE } from './config.js';
import { meetingContext, currentVideoQuality, uiState } from './state.js';
import { createParticipant, getLocalParticipant, setLocalParticipant } from './participants.js';
import { postToHost } from './messaging.js';

// ===== MEDIA CONSTRAINTS BUILDER =====

/**
 * Build media constraints based on meetingContext and quality
 */
export function buildMediaConstraints(quality = currentVideoQuality) {
    const qualityPreset = VIDEO_QUALITY_PRESETS[quality] || VIDEO_QUALITY_PRESETS.medium;

    // Audio constraints
    let audioConstraints = false;
    if (meetingContext.audioEnabled) {
        audioConstraints = { ...AUDIO_CONSTRAINTS_BASE };
        if (meetingContext.micId) {
            audioConstraints.deviceId = { ideal: meetingContext.micId };
        }
    }

    // Video constraints
    let videoConstraints = false;
    if (meetingContext.videoEnabled) {
        videoConstraints = {
            width: qualityPreset.width,
            height: qualityPreset.height,
            frameRate: qualityPreset.frameRate,
            facingMode: 'user',
            aspectRatio: { ideal: 16 / 9 }
        };
        if (meetingContext.cameraId) {
            videoConstraints.deviceId = { ideal: meetingContext.cameraId };
        }
    }

    const constraints = {
        audio: audioConstraints,
        video: videoConstraints
    };

    console.log(`Built media constraints with ${quality} quality:`, constraints);
    return constraints;
}

// ===== LOCAL MEDIA INITIALIZATION =====

/**
 * Initialize local media (getUserMedia)
 */
export async function initLocalMedia(constraints = { audio: true, video: true }, retryWithLowerQuality = true) {
    console.log('initLocalMedia called with constraints:', constraints);

    // Special case: both disabled
    if (constraints.audio === false && constraints.video === false) {
        console.log('Both audio and video disabled - creating participant without stream');
        return createLocalParticipantWithoutStream();
    }

    try {
        const stream = await navigator.mediaDevices.getUserMedia(constraints);
        console.log('getUserMedia success! Stream:', stream);
        logStreamInfo(stream);

        const localName = getLocalParticipantName();
        const audioTracks = stream.getAudioTracks();
        const videoTracks = stream.getVideoTracks();

        const isMutedState = audioTracks.length === 0 || !audioTracks.some(t => t.enabled);
        const isVideoOffState = videoTracks.length === 0 || !videoTracks.some(t => t.enabled);

        const local = createParticipant({
            id: LOCAL_ID,
            name: localName,
            avatar: '👤',
            isSpeaking: false,
            isMuted: isMutedState,
            isVideoOff: isVideoOffState,
            stream: stream
        });

        setLocalParticipant(local);

        // Update UI state
        uiState.isMuted = local.isMuted;
        uiState.isVideoOff = local.isVideoOff;

        // Setup track event listeners
        setupTrackListeners(stream, local);

        postToHost({ type: 'permission-granted' });

        return local;
    } catch (err) {
        console.error('getUserMedia error', err);
        return handleGetUserMediaError(err, constraints, retryWithLowerQuality);
    }
}

/**
 * Change video quality on the fly
 */
export async function changeVideoQuality(quality) {
    if (!VIDEO_QUALITY_PRESETS[quality]) {
        console.error('Invalid quality preset:', quality);
        return;
    }

    console.log('Changing video quality to:', quality);

    const local = getLocalParticipant();
    if (local && local.stream) {
        local.stream.getTracks().forEach(track => track.stop());
    }

    const constraints = buildMediaConstraints(quality);
    await initLocalMedia(constraints);

    postToHost({ type: 'quality-changed', quality: quality });
}

// ===== HELPER FUNCTIONS =====

function getLocalParticipantName() {
    return meetingContext.userName
        ? `${meetingContext.userName} (${meetingContext.isHost ? 'Host' : 'Bạn'})`
        : 'Bạn';
}

function createLocalParticipantWithoutStream() {
    const localName = getLocalParticipantName();
    const local = createParticipant({
        id: LOCAL_ID,
        name: localName,
        avatar: '👤',
        isSpeaking: false,
        isMuted: true,
        isVideoOff: true,
        stream: null
    });
    setLocalParticipant(local);
    uiState.isMuted = true;
    uiState.isVideoOff = true;
    postToHost({ type: 'permission-granted' });
    return local;
}

function logStreamInfo(stream) {
    console.log('Video tracks:', stream.getVideoTracks());
    console.log('Audio tracks:', stream.getAudioTracks());

    if (stream.getVideoTracks().length > 0) {
        const videoTrack = stream.getVideoTracks()[0];
        const settings = videoTrack.getSettings();
        console.log('Actual video settings:', {
            width: settings.width,
            height: settings.height,
            frameRate: settings.frameRate,
            facingMode: settings.facingMode
        });
    }
}

function setupTrackListeners(stream, local) {
    stream.getAudioTracks().forEach(t => {
        t.addEventListener('ended', () => {
            local.isMuted = true;
        });
        t.addEventListener('mute', () => {
            local.isMuted = true;
        });
        t.addEventListener('unmute', () => {
            local.isMuted = !stream.getAudioTracks().some(x => x.enabled);
        });
    });

    stream.getVideoTracks().forEach(t => {
        t.addEventListener('ended', () => {
            local.isVideoOff = true;
        });
        t.addEventListener('mute', () => {
            local.isVideoOff = true;
        });
        t.addEventListener('unmute', () => {
            local.isVideoOff = !stream.getVideoTracks().some(x => x.enabled);
        });
    });
}

async function handleGetUserMediaError(err, constraints, retryWithLowerQuality) {
    console.error('Error details:', {
        name: err.name,
        message: err.message,
        constraint: err.constraint
    });

    if (retryWithLowerQuality) {
        console.warn('Retrying with lower quality constraints...');
        const fallbackQualities = ['low', 'basic'];

        for (const fallbackQuality of fallbackQualities) {
            try {
                console.log(`Attempting ${fallbackQuality} quality...`);
                let fallbackConstraints;

                if (fallbackQuality === 'basic') {
                    fallbackConstraints = { audio: true, video: true };
                } else {
                    fallbackConstraints = buildMediaConstraints(fallbackQuality);
                }

                return await initLocalMedia(fallbackConstraints, false);
            } catch (retryErr) {
                console.error(`${fallbackQuality} quality also failed:`, retryErr.message);
            }
        }
    }

    // All attempts failed
    postToHost({ type: 'permission-denied', msg: err.message });
    return createLocalParticipantWithoutStream();
}
