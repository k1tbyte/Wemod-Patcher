const { app, BrowserWindow } = require('electron')
const path = require('path')

app.disableHardwareAcceleration()
app.commandLine.appendSwitch('disable-http-cache')

function createWindow () {
    const win = new BrowserWindow({
        width: 800,
        height: 600,
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false,
            spellcheck: false
        },
        autoHideMenuBar: true
    })

    win.loadFile('index.html')
}

app.whenReady().then(createWindow)

app.on('window-all-closed', () => {
    app.quit()
})