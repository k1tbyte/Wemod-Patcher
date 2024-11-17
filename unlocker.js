const path = require('path');
const fs = require('fs');
const readline = require('readline');
const asar = require('asar');
const patchBySignature = require("./memoryScanner");


const regex = /getUserAccount\(\)\{.*?return\s+this\.#\w+\.fetch\(\{.*?\}\).*?\}/g;
const asarPatch = "getUserAccount(){return this.#v.fetch({endpoint:\"/v3/account\",method:\"GET\",name:\"/v3/account\",collectMetrics:0}).then(response=>{response.subscription={period:\"yearly\",state:\"active\"};response.flags=78;return response;})}"
const signature = "E8 ?? ?? ?? ?? 85 C0 75 ?? F6 C3 01 74 ?? 48 89 F9 E8 ?? ?? ?? ??"
const patchBytes = [0x31]
const patchOffset = 0x5
// ...
// test eax, eax (0x85 for	r/m16/32/64)
// jnz      short loc_1403A4DD2 (Integrity check failed)
// call    near ptr funk_1445527E0
// ...

console.log("WeMod unlocker by K1tbyte")
let defaultDir = path.join(process.env.LOCALAPPDATA || path.join(process.env.HOME || process.env.USERPROFILE, 'AppData', 'Local'), 'WeMod');
let appDir = null

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
});

const read = (title, onSubmit, onExit) => {
    rl.question(title, (answer) => {
        try {
            onSubmit(answer)
        } finally {
            rl.close()
            onExit?.()
        }
    });
}

const checkWeModPaths = (dir) => {
    return fs.existsSync(dir) && fs.existsSync(path.join(dir, 'WeMod.exe')) && fs.existsSync(path.join(dir, 'resources'))
}

const patchAsar = unpackedPath => {
    let items = fs.readdirSync(unpackedPath,{ withFileTypes: true })
    items = items.filter(item => !item.isDirectory() && /^app-\w+/.test(item.name));
    if(items.length === 0) {
        console.log(" - No app bundle found")
        return;
    }

    let asarPatchApplied = false;
    for (const item of items) {
        const data = fs.readFileSync(path.join(unpackedPath, item.name), { encoding: 'utf8'})

        const matches = data.match(regex)
        if(!matches)  {
            continue;
        }
        if(matches.length > 1) {
            console.error("   - Multiple target functions found. Looks like the version is not supported")
            throw new Error("Multiple target functions found");
        }
        console.log("   - Found target function in: ", item.name)
        console.log("   - Patching asar...")
        fs.writeFileSync(path.join(unpackedPath, item.name), data.replace(regex, asarPatch), {encoding: 'utf8'})
        console.log("   - Patch applied")
        asarPatchApplied = true;
        break;
    }

    if(!asarPatchApplied) {
        throw new Error("Failed to apply patch");
    }
}

const patchPE = async () => {
    console.log(" - Patching PE...")
    const pePath = path.join(appDir, 'WeMod.exe')
    const procStart = await patchBySignature(pePath, signature, patchBytes, patchOffset)
    if(procStart === -1) {
        console.log("   - Signature not found or already patched")
        return;
    }
    console.log("   - Patch saved")
}

const start = async () => {
    console.log(" - Extracting asar...")
    try {
        const asarPath = path.join(appDir, 'resources', 'app.asar')
        asar.extractAll(asarPath, path.join(appDir, 'resources', 'app.asar.unpacked'))
    } catch(e) {
        console.error("Failed to extract asar", e)
        return;
    }

    const unpackedPath = path.join(appDir, 'resources', 'app.asar.unpacked')

    fs.renameSync(path.join(appDir, 'resources', 'app.asar'), path.join(appDir, 'resources', 'app.asar.backup'))
    console.log("   - Backup saved")
    patchAsar(unpackedPath)

    await asar.createPackageWithOptions(unpackedPath, path.join(appDir, 'resources', 'app.asar'),
        { unpack: path.join(unpackedPath,"static/unpacked/**") })
    console.log("   - Patch saved")

    await patchPE()
    console.log("Done!")
    process.exit(0)
}

const prepare = async () => {
    console.log(" - Path: ", appDir || "Not found")

    if (!appDir) {
        read("WeMod directory not found. Enter the path manually: ", (answer) => {
            if (checkWeModPaths(answer)) {
                appDir = answer;
                return;
            }
            console.log("Invalid path")
        }, prepare);
        return;
    }

    read("Continue? (y/n): ", (answer) => {
        if(answer.toLowerCase() === 'y') {
            start()
        }
    })
}

if(fs.existsSync(defaultDir)) {
    const items = fs.readdirSync(defaultDir, { withFileTypes: true });
    const appFolders = items
        .filter(item => item.isDirectory() && /^app-\w+/.test(item.name))
        .map(item => {
            const folderPath = path.join(defaultDir, item.name);
            const stats = fs.statSync(folderPath); // Получаем информацию о папке
            return {
                name: item.name,
                path: folderPath,
                mtime: stats.mtime,
            };
        });

    appFolders.sort((a, b) => b.mtime - a.mtime);
    for(const folder of appFolders) {
        if(checkWeModPaths(folder.path)) {
            appDir = folder.path;
            break;
        }
    }
}


(async () => {
    await prepare();
})();
