const { app } = require("electron");
const fs = require("fs");
const path = require("path");
const { exec } = require("child_process");

class GitHubUpdater {
    constructor(owner, repo, currentVersion) {
        this.owner = owner;
        this.repo = repo;
        this.currentVersion = currentVersion || app.getVersion();
        this.apiUrl = `https://api.github.com/repos/${owner}/${repo}/releases/latest`;
        this.downloadDir = path.join(app.getPath("temp"), "update");
    }

    async checkForUpdates() {
        const response = await fetch(this.apiUrl, {
            headers: { "User-Agent": "GitHub-Updater" },
        });
        if (!response.ok) {
            throw new Error(`An error occurred while checking for an update: ${response.statusText}`);
        }

        const release = await response.json();
        const latestVersion = release.tag_name;
        const assets = release.assets;

        if (this.currentVersion === latestVersion) {
            return null;
        }

        const asset = assets.find(a => a.name.endsWith(".exe"));
        if (!asset) {
            throw new Error("Unable to find files to update");
        }

        return {
            version: latestVersion,
            url: asset.browser_download_url,
            name: asset.name,
        };
    }

    async downloadUpdate(updateInfo) {
        const response = await fetch(updateInfo.url);
        if (!response.ok) {
            throw new Error(`Error downloading file: ${response.statusText}`);
        }

        if (!fs.existsSync(this.downloadDir)) {
            fs.mkdirSync(this.downloadDir, { recursive: true });
        }

        const filePath = path.join(this.downloadDir, updateInfo.name);
        const fileStream = fs.createWriteStream(filePath);

        await new Promise((resolve, reject) => {
            const downloadStream = response.body.getReader();

            const pump = async () => {
                try {
                    while (true) {
                        const { done, value } = await downloadStream.read();
                        if (done) {
                            fileStream.end();
                            resolve();
                            break;
                        }
                        fileStream.write(value);
                    }
                } catch (error) {
                    reject(error);
                }
            };

            fileStream.on("error", (error) => {
                reject(new Error(`File write error: ${error.message}`));
            });

            pump();
        });

        return filePath;
    }

    async applyUpdate(filePath) {
        try {
            const currentExecutable = process.env.PORTABLE_EXECUTABLE_FILE;
            const updateScript = `Start-Sleep -Seconds 3; Copy-Item -Path '${filePath}' -Destination '${currentExecutable}' -Force; Remove-Item -Path '${filePath}' -Force; Start-Sleep -Seconds 2; Start-Process -FilePath '${currentExecutable}';`;

            exec(`start /b "" powershell -WindowStyle Hidden -Command "${updateScript}"`, {
                windowsHide: true,
                stdio: 'ignore'
            });

            setTimeout(() => {
                app.quit();
            }, 1000);

        } catch (error) {
            throw new Error(`Update failed: ${error.message}`);
        }
    }
}

module.exports = GitHubUpdater;
