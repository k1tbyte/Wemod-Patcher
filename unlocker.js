const path = require('path');
const fs = require('fs');
const asar = require('asar');
const { execSync  } = require("child_process");
const patchBySignature = require("./memoryScanner");


const regex = /getUserAccount\(\)\{.*?return\s+this\.#\w+\.fetch\(\{.*?}\).*?}/g;
const asarPatch = "getUserAccount(){return this.#<fetch_field_name>.fetch({endpoint:\"/v3/account\",method:\"GET\",name:\"/v3/account\",collectMetrics:0}).then(response=>{response.subscription={period:\"yearly\",state:\"active\"};response.flags=78;return response;})}"
const signature = "E8 ?? ?? ?? ?? ?? C0 75 ?? F6 C3 01 74 ?? 48 89 F9 E8 ?? ?? ?? ??"
const patchBytes = [0x31]
const patchOffset = 0x5
// ...
// test eax, eax (0x85 for	r/m16/32/64)
// jnz      short loc_1403A4DD2 (Integrity check failed)
// call    near ptr funk_1445527E0
// ...

class Unlocker {

    constructor(appDir, logger) {
        this.appDir = appDir;
        this.logger = logger;
    }

    #getFetchFieldName(code) {
        const match = code.match(/this\.#([a-zA-Z_$][0-9a-zA-Z_$]*)\.fetch/);
        return match ? match[1] : null;
    }

    #patchAsar (unpackedPath) {
        let items = fs.readdirSync(unpackedPath,{ withFileTypes: true })
        items = items.filter(item => !item.isDirectory() && /^app-\w+/.test(item.name));
        if(items.length === 0) {
            throw new Error(" - No app bundle found");
        }

        let asarPatchApplied = false;
        for (const item of items) {
            const data = fs.readFileSync(path.join(unpackedPath, item.name), { encoding: 'utf8'})

            const matches = data.match(regex)
            if(!matches)  {
                continue;
            }
            if(matches.length > 1) {
                throw new Error("   - Multiple target functions found. Looks like the version is not supported");
            }

            const fetchFieldName = this.#getFetchFieldName(matches[0]);
            if(!fetchFieldName) {
                throw new Error("   - Fetch field name not found");
            }

            const patch = asarPatch.replace(/<fetch_field_name>/g, fetchFieldName)

            this.logger("   - Found target function in: " + item.name)
            this.logger("   - Patching asar...")
            fs.writeFileSync(path.join(unpackedPath, item.name), data.replace(regex, patch), {encoding: 'utf8'})
            this.logger("   - Patch applied")
            asarPatchApplied = true;
            break;
        }

        if(!asarPatchApplied) {
            throw new Error("Failed to apply patch");
        }
    }


    async #patchPE () {
        this.logger(" - Patching PE...")
        const pePath = path.join(this.appDir, 'WeMod.exe')
        const procStart = await patchBySignature(pePath, signature, patchBytes, patchOffset)
        if(procStart === -1) {
            throw new Error("   - Signature not found or already patched")
        }

        this.logger(procStart === 0 ? "   - PE already patched" : "   - Patch saved")
    }

    async start () {
        this.logger(" - Extracting asar...")
        const asarPath = path.join(this.appDir, 'resources', 'app.asar')
        const unpackedPath = path.join(this.appDir, 'resources', 'app.asar.unpacked')
        const backupPath = path.join(this.appDir, 'resources', 'app.asar.backup')

        if(fs.existsSync(backupPath)) {
            this.logger("   - Backup already exists")
        } else {
            execSync(`copy "${asarPath}" "${backupPath}"`, { encoding: "utf-8" });
            this.logger("   - Backup saved")
        }

        try {
            asar.extractAll(asarPath, unpackedPath)
        } catch(e) {
            throw new Error("Failed to extract asar: " + e)
        }

        this.#patchAsar(unpackedPath)

        await asar.createPackageWithOptions(unpackedPath, asarPath,
            { unpack: path.join(unpackedPath,"static/unpacked/**") })
        this.logger("   - Patch saved")

        await this.#patchPE()
    }
}

module.exports = Unlocker;

