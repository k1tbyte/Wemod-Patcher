const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const fs = require("fs");
const Unlocker = require("./unlocker");
const GitHubUpdater = require("./updater");

const packageJsonPath = path.join(__dirname, 'package.json');
const packageData = require(packageJsonPath)
const updater = new GitHubUpdater(packageData.author, packageData.name);


function createWindow() {
    const win = new BrowserWindow({
        width: 800,
        height: 600,
        resizable: false,
        autoHideMenuBar: true,
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false
        }
    });

    win.loadFile('index.html');
}

const checkWeModPath = (root) => fs.existsSync(path.join(root, 'WeMod.exe')) &&
    fs.existsSync(path.join(root, 'resources/app.asar'))

function log(message, type = 'info') {
    BrowserWindow.getAllWindows().forEach(win => {
        win.webContents.send('log', { message, type });
    });
}

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

ipcMain.handle('select-file', async () => {
    const result = await dialog.showOpenDialog({
        properties: ["openDirectory"],
        defaultPath: process.env.LOCALAPPDATA || path.join(process.env.HOME || process.env.USERPROFILE, 'AppData', 'Local')
    });

    if (!result.canceled && result.filePaths.length > 0) {
        return {
            filePath: result.filePaths[0],
            fileName: path.basename(result.filePaths[0]),
            valid: checkWeModPath(result.filePaths[0])
        };
    }
    return null;
});

ipcMain.handle('apply-patch', async (event, path) => {
    const unlocker = new Unlocker(path, (e) => log(e))
    await unlocker.start()
})

ipcMain.handle('resolve-default-path', async () => {
    const defaultDir = path.join(process.env.LOCALAPPDATA || path.join(process.env.HOME || process.env.USERPROFILE, 'AppData', 'Local'), 'WeMod');

    if (!fs.existsSync(defaultDir)) {
        return null;
    }

    const items = fs.readdirSync(defaultDir, {withFileTypes: true});
    const appFolders = items
        .filter(item => item.isDirectory() && /^app-\w+/.test(item.name))
        .map(item => {
            const folderPath = path.join(defaultDir, item.name);
            const stats = fs.statSync(folderPath);
            return {
                name: item.name,
                path: folderPath,
                mtime: stats.mtime,
            };
        });

    let appDir = null;
    appFolders.sort((a, b) => b.mtime - a.mtime);
    for (const folder of appFolders) {
        if (checkWeModPath(folder.path)) {
            appDir = folder.path;
            break;
        }
    }

    return appDir;
})

ipcMain.handle('start-patch', async (event, filePath) => {
    try {
        // stub
        return {
            success: true,
            message: 'Patch completed successfully!'
        };
    } catch (error) {
        return {
            success: false,
            message: error.message
        };
    }
});

ipcMain.handle("get-current-version", () => {
    return packageData.version
})


ipcMain.handle("check-updates", async () => {
    return await updater.checkForUpdates()
})

ipcMain.on('open-link', () => {
    require('electron').shell.openExternal("https://github.com/k1tbyte/Wemod-Patcher")
})

ipcMain.on("apply-update", async (event, source) => {
    try {
        log("Downloading update ...")
        const path = await updater.downloadUpdate(source)
        log("Preparation")
        updater.applyUpdate(path)
    } catch (err) {
        log(err, "error")
    }
})