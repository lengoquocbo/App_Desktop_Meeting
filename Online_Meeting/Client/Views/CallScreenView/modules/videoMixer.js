let canvas = null;
let ctx = null;
let animationFrameId = null;

export function createMixedStream(screenStream, cameraStream) {
    canvas = document.createElement('canvas');
    ctx = canvas.getContext('2d');

    const screenVideo = document.createElement('video');
    screenVideo.srcObject = screenStream;
    screenVideo.muted = true; // Bắt buộc muted mới autoplay được
    screenVideo.autoplay = true;
    screenVideo.playsInline = true; // Hỗ trợ tốt hơn trên một số trình duyệt

    const cameraVideo = document.createElement('video');
    cameraVideo.srcObject = cameraStream;
    cameraVideo.muted = true;
    cameraVideo.autoplay = true;
    cameraVideo.playsInline = true;

    // Capture 30 FPS
    const mixedStream = canvas.captureStream(30);

    screenVideo.onloadedmetadata = () => {
        canvas.width = screenVideo.videoWidth;
        canvas.height = screenVideo.videoHeight;

        // ✅ FIX QUAN TRỌNG: Gọi play() rõ ràng để bắt đầu render
        screenVideo.play().catch(e => console.error("Lỗi play screen:", e));
        cameraVideo.play().catch(e => console.error("Lỗi play camera:", e));

        drawLoop(screenVideo, cameraVideo);
    };

    return mixedStream;
}

function drawLoop(screenVideo, cameraVideo) {
    if (!ctx || !canvas) return;

    // Vẽ nền Màn hình
    ctx.drawImage(screenVideo, 0, 0, canvas.width, canvas.height);

    // Vẽ Camera góc phải dưới
    // Kiểm tra readyState >= 2 (HAVE_CURRENT_DATA) là đủ để vẽ
    if (cameraVideo.readyState >= 2) {
        const camWidth = canvas.width * 0.20;
        const camHeight = camWidth * (9 / 16);
        const x = canvas.width - camWidth - 20;
        const y = canvas.height - camHeight - 20;

        // Vẽ viền
        ctx.strokeStyle = "#fff";
        ctx.lineWidth = 2;
        ctx.strokeRect(x, y, camWidth, camHeight);

        // Vẽ hình
        ctx.drawImage(cameraVideo, x, y, camWidth, camHeight);
    }

    animationFrameId = requestAnimationFrame(() => drawLoop(screenVideo, cameraVideo));
}

export function stopMixing() {
    if (animationFrameId) cancelAnimationFrame(animationFrameId);
    canvas = null;
    ctx = null;
}