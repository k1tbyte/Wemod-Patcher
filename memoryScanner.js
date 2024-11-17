const fs = require("fs");

function findPatternInBuffer(buffer, bytesRead, signature, mask) {
    const bufferLength = bytesRead + signature.length - 1;

    for (let i = 0; i <= bytesRead - signature.length; i++) {
        if (isMatch(buffer, signature, mask, i)) return i;
    }

    return -1;
}


function isMatch(buffer, signature, mask, offset) {
    for (let i = 0; i < signature.length; i++) {
        if (mask[i] === "x" && buffer[offset + i] !== signature[i]) return false;
    }
    return true;
}

function parseSignature(signature) {
    const signatureBytes = [];
    let mask = "";

    const tokens = signature.split(" ");
    tokens.forEach((token) => {
        if (token === "??" || token === "?") {
            signatureBytes.push(0);
            mask += "?";
        } else {
            signatureBytes.push(parseInt(token, 16));
            mask += "x";
        }
    });

    return { signature: Buffer.from(signatureBytes), mask };
}


async function patchBySignature(filePath, functionSignature, patchBytes, patchOffset) {
    const { signature, mask } = parseSignature(functionSignature);

    const bufferSize = 8192;
    const buffer = Buffer.alloc(bufferSize + signature.length - 1);

    const fileHandle = await fs.promises.open(filePath, "r+");

    let filePosition = 0;
    try {
        while (true) {
            const { bytesRead } = await fileHandle.read(buffer, 0, bufferSize, filePosition);
            if (bytesRead === 0) break;

            const matchIndex = findPatternInBuffer(buffer, bytesRead, signature, mask);
            if (matchIndex !== -1) {
                const functionStartPosition = filePosition + matchIndex;

                // Go to patch position
                await fileHandle.write(Buffer.from(patchBytes), 0, patchBytes.length, functionStartPosition + patchOffset);

                return functionStartPosition; // Return the address of the function start by signature
            }

            filePosition += bytesRead;
            buffer.copy(buffer, 0, bufferSize, bufferSize + signature.length - 1);
        }
    } finally {
        await fileHandle.close();
    }

    return -1;
}

module.exports = patchBySignature;