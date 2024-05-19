import subprocess
import zipfile
import json
import yaml
import datetime
import sys


RELEASE_DIR = "Jellyfin.Plugin.CinemaMode/bin/release/net8.0/"
RELEASE_DLL = "Jellyfin.Plugin.CinemaMode/bin/release/net8.0/Jellyfin.Plugin.CinemaMode.dll"
RELEASE_META = "Jellyfin.Plugin.CinemaMode/bin/release/net8.0/meta.json"


class Meta:
    """ Meta """

    category: str
    changelog: str
    description: str
    guid: str
    imageUrl: str
    name: str
    overview: str
    owner: str
    targetAbi: str
    timestamp: str
    version: str
    zip_file_name: str

    def __init__(
            self,
            category,
            changelog,
            description,
            guid,
            imageUrl,
            name,
            overview,
            owner,
            targetAbi,
            timestamp,
            version,
            zip_file_name,
        ) -> None:
        self.category=category
        self.changelog=changelog
        self.description=description
        self.guid=guid
        self.imageUrl=imageUrl
        self.name=name
        self.overview=overview
        self.owner=owner
        self.targetAbi=targetAbi
        self.timestamp=timestamp
        self.version=version
        self.zip_file_name=zip_file_name

    @classmethod
    def get_meta(cls) -> "Meta":

        # Load build.yml
        with open("build.yaml", "r") as yml:
            build_yaml = yaml.safe_load(yml)
        
        return Meta(
            category=build_yaml["category"],
            changelog=build_yaml["changelog"],
            description=build_yaml["description"],
            guid=build_yaml["guid"],
            imageUrl="https://github.com/CherryFloors/jellyfin-plugin-cinemamode/raw/main/Jellyfin.Plugin.CinemaMode/Images/jellyfin-plugin-cinemamode.png",
            name=build_yaml["name"],
            overview=build_yaml["overview"],
            owner=build_yaml["owner"],
            targetAbi=build_yaml["targetAbi"],
            timestamp=datetime.datetime.now().isoformat(),
            version=build_yaml["version"],
            zip_file_name=f'cinemamode_{build_yaml["version"]}.zip',
        )
        
       
    def save_file(self) -> None:
        out_dict = {
            "category": self.category,
            "changelog": self.changelog,
            "description": self.description,
            "guid": self.guid,
            "imageUrl": self.imageUrl,
            "name": self.name,
            "overview": self.overview,
            "owner": self.owner,
            "targetAbi": self.targetAbi,
            "timestamp": self.timestamp,
            "version": self.version,
        }
        with open(RELEASE_META, "w") as f:
            f.write(json.dumps(out_dict, indent=4))


class ManifestEntry:
    """ ManifestEntry """

    checksum: str
    changelog: str
    targetAbi: str
    sourceUrl: str
    timestamp: str
    version: str

    def __init__(
            self,
            checksum,
            changelog,
            targetAbi,
            sourceUrl,
            timestamp,
            version,
        ) -> None:
        self.checksum=checksum
        self.changelog=changelog
        self.targetAbi=targetAbi
        self.sourceUrl=sourceUrl
        self.timestamp=timestamp
        self.version=version


    @classmethod
    def get_manifest_entry(cls, meta: Meta) -> "ManifestEntry":

        checksum_args = ["md5sum", f"{RELEASE_DIR}/{meta.zip_file_name}"]
        ProcOutput = subprocess.run(checksum_args, capture_output=True, text=True)
        _checksum = ProcOutput.stdout.split(" ")[0]
        
        return ManifestEntry(
            checksum=_checksum,
            changelog=meta.changelog,
            targetAbi=meta.targetAbi,
            sourceUrl=f"https://github.com/CherryFloors/jellyfin-plugin-cinemamode/releases/download/v{meta.version}/{meta.zip_file_name}",
            timestamp=meta.timestamp,
            version=meta.version,
        )
    
    def to_output_dict(self) -> dict:
        return {
            "checksum": self.checksum,
            "changelog": self.changelog,
            "targetAbi": self.targetAbi,
            "sourceUrl": self.sourceUrl,
            "timestamp": self.timestamp,
            "version": self.version,
        }
    
    def add_manifest_entry(self) -> None:
        with open("manifest.json", "r") as f:
            manifest = json.load(f)
        
        versions = [i["version"] for i in manifest[0]["versions"]]
        if self.version in versions:
            print("Current version is already in the manifest")
            sys.exit()

        
        updated_versions = [self.to_output_dict()] + manifest[0]["versions"]
        manifest[0]["versions"] = updated_versions

        with open("manifest.json", "w") as f:
            f.write(json.dumps(manifest, indent=4))




def build() -> None:
    build_args = ["dotnet", "publish", "./Jellyfin.Plugin.CinemaMode/Jellyfin.Plugin.CinemaMode.csproj", "--configuration", "release", "--output", "bin"]
    ProcOutput = subprocess.run(build_args, capture_output=True, text=True)
    print(ProcOutput.stdout)
    print(ProcOutput.stderr)

def package(meta: Meta) -> None:
    # Get meta data
    with zipfile.ZipFile(f"{RELEASE_DIR}/{meta.zip_file_name}", "w") as zip:
        zip.write(RELEASE_DLL, "Jellyfin.Plugin.CinemaMode.dll")
        zip.write(RELEASE_META, "meta.json")


if __name__ == "__main__":

    # Build project and export meta
    print("Building project...")
    build()
    print("Exporting metadata...")
    project_meta_data = Meta.get_meta()
    project_meta_data.save_file()

    # Create the package
    print("Creating the package...")
    package(meta=project_meta_data)

    # Update the manifest
    print("Updating manifest...")
    manifest_entry = ManifestEntry.get_manifest_entry(meta=project_meta_data)
    manifest_entry.add_manifest_entry()
    print("Done.")

