const { ipcRenderer } = require('electron');

class PatcherUI {
    constructor() {
        this.selectedPath = '';
        this.initializeElements();
        this.bindEvents();
    }

    initializeElements() {
        this.filePathInput = document.getElementById('file-path');
        this.browseBtn = document.getElementById('browse-btn');
        this.patchBtn = document.getElementById('patch-btn');
        this.logSection = document.querySelector('.log-section');
    }

    bindEvents() {
        this.browseBtn.addEventListener('click', () => this.selectFile());
        this.patchBtn.addEventListener('click', () => this.startPatch());
    }

    setPath(path) {
        this.filePathInput.value = this.selectedPath = path
        this.patchBtn.disabled = !path;
    }

    async selectFile() {
        try {
            const result = await ipcRenderer.invoke('select-file');
            if (!result?.filePath) {
                return;
            }

            if(!result.valid) {
                this.addLog(`The folder “${result.filePath}” is not recognized as a Wemod directory`, 'error')
                return;
            }

            this.setPath(result.filePath)
            this.addLog('Folder selected: ' + result.fileName, 'info');
        } catch (error) {
            this.addLog('Error selecting file: ' + error.message, 'error');
        }
    }

    addLog(message, type = 'info') {
        const entry = document.createElement('div');
        entry.className = `log-entry ${type}`;
        entry.textContent = `[${new Date().toLocaleTimeString()}] ${message}`;
        this.logSection.appendChild(entry);
        this.logSection.scrollTop = this.logSection.scrollHeight;
    }

    async startPatch() {
        if (!this.selectedPath) return;

        this.patchBtn.disabled = true;
        this.addLog('Starting patch process...', 'info');

        try {
            const result = await ipcRenderer.invoke('start-patch', this.selectedPath);
            this.addLog(result.message, result.success ? 'success' : 'error');
        } catch (error) {
            this.addLog('Patch failed: ' + error.message, 'error');
        } finally {
            this.patchBtn.disabled = false;
        }
    }

     resolveDefault() {
        ipcRenderer.invoke("resolve-default-path").then(path => {
            this.addLog(path ? 'The WeMod folder has been found!' :
                "WeMod folder was not found. You need to specify the path manually", path ? "success" : "warning"
            );
            this.setPath(path)
        })
    }
}

window.addEventListener('DOMContentLoaded', async() => {
    const ui = new PatcherUI();
    await ui.resolveDefault()
});