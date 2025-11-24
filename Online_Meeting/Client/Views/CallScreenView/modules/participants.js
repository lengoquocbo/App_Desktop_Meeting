import { LOCAL_ID } from './config.js';
import { meetingContext } from './state.js';

// ===== PARTICIPANTS DATA STRUCTURES =====
export const participantsById = new Map();
// ===== PARTICIPANT FACTORY =====
export function createParticipant({
    id, name, avatar = '👤', isSpeaking = false, isMuted = false, isVideoOff = true, stream = null,
    isScreenSharing = false,
    connectionId = null
}) {
    return {
        id, name, avatar, isSpeaking, isMuted, isVideoOff, stream,
        isScreenSharing,
        connectionId,
        videoEl: null
    };
}

// ===== PARTICIPANT OPERATIONS =====

/**
 * Get all participants as array (local first)
 */
export function getCurrentParticipantsArray() {
    return Array.from(participantsById.values());
}

/**
 * Add a remote participant
 */
export function addRemoteParticipant({
    id, name, avatar = '👤', isMuted = false, isVideoOff = false, stream = null, connectionId = null
}) {
    if (participantsById.has(id)) {
        console.log(`Participant ${name} already exists. Updating info only.`);
        const p = participantsById.get(id);
        p.name = name;
        p.isMuted = isMuted;
        p.isVideoOff = isVideoOff;
        if (connectionId) p.connectionId = connectionId;
        if (stream) p.stream = stream;
        return p;
    }

    const p = createParticipant({ id, name, avatar, isMuted, isVideoOff, stream, connectionId });
    participantsById.set(id, p);
    console.log(`Added NEW remote participant: ${name} (${id})`);
    return p;
}

/**
 * Remove a participant
 */
export function removeParticipant(id) {
    const p = participantsById.get(id);
    if (!p) return;

    // Stop tracks if any
    if (p.stream) {
        try {
            p.stream.getTracks().forEach(t => t.stop());
        } catch (err) {
            console.error('Error stopping tracks:', err);
        }
    }

    participantsById.delete(id);
    console.log(`Removed participant: ${id}`);
}

/**
 * Update participant media state
 */
export function updateParticipantMedia(id, {
    isMuted = undefined,
    isVideoOff = undefined,
    isSpeaking = undefined,
    stream = undefined,
    isScreenSharing = undefined,
    secondaryStream = undefined
}) {
    console.log(`[DEBUG]  Updating media for participant: ${id}`);
    const p = participantsById.get(id);
    if (!p) {
        console.warn('Participant not found:', id);
        return;
    }

    if (typeof isMuted === 'boolean') p.isMuted = isMuted;
    if (typeof isVideoOff === 'boolean') p.isVideoOff = isVideoOff;
    if (typeof isSpeaking === 'boolean') p.isSpeaking = isSpeaking;
    if (stream !== undefined) p.stream = stream;
    if (secondaryStream !== undefined) {
        p.secondaryStream = secondaryStream;
        console.log(`Updated secondary stream for participant: ${id}`);
    }
    if (typeof isScreenSharing === 'boolean') {
        p.isScreenSharing = isScreenSharing;
        console.log(`Participant ${id} screen sharing: ${isScreenSharing}`);
    }

    return p;
}

/**
 * Get local participant
 */
export function getLocalParticipant() {
    return participantsById.get(LOCAL_ID);
}

/**
 * Set local participant
 */
export function setLocalParticipant(participant) {
    participantsById.set(LOCAL_ID, participant);
}

/**
 * Handle participants update from SignalR
 */
export function handleParticipantsUpdate(participants) {
    console.log('handleParticipantsUpdate received:', participants);

    if (!participants || !Array.isArray(participants)) {
        console.warn('Invalid participants data');
        return;
    }

    console.log('🟢 [DEBUG] Total participants after update:', participants.size);

    // Get current participant IDs (excluding local)
    const currentRemoteIds = new Set(
        Array.from(participantsById.keys()).filter(id => id !== LOCAL_ID)
    );

    // Track received participant IDs
    const receivedIds = new Set();

    // Add or update participants
    participants.forEach(p => {
        // Skip if this is the local user
        if (p.userId === meetingContext.userId || p.userId === LOCAL_ID) {
            return;
        }

        receivedIds.add(p.userId);

        // Check if participant already exists
        if (participantsById.has(p.userId)) {
            // Update existing participant
            console.log('Updating existing participant:', p.userId);
            const existing = participantsById.get(p.userId);
            if (existing) {
                if (p.isMuted !== undefined) existing.isMuted = p.isMuted;
                if (p.isVideoOff !== undefined) existing.isVideoOff = p.isVideoOff;
                if (p.username) existing.name = p.username;
                if (p.connectionId) existing.connectionId = p.connectionId;
            }
        } else {
            // New participant - add them
            console.log('Adding new participant:', p.userId, p.username);
            addRemoteParticipant({
                id: p.userId,
                name: p.username || 'Unknown',
                avatar: '👤',
                isMuted: p.isMuted !== undefined ? p.isMuted : false,
                isVideoOff: p.isVideoOff !== undefined ? p.isVideoOff : false,
                stream: null
            });
        }
    });

    // Remove participants that are no longer in the list
    currentRemoteIds.forEach(id => {
        if (!receivedIds.has(id)) {
            console.log('Removing participant:', id);
            removeParticipant(id);
        }
    });
}

    /**
     * Clear all participants
     */
    export function clearAllParticipants() {
        participantsById.forEach((p, id) => {
            if (p.stream) {
                try {
                    p.stream.getTracks().forEach(t => t.stop());
                } catch (err) {
                    console.error('Error stopping tracks:', err);
                }
            }
        });
        participantsById.clear();
    }
