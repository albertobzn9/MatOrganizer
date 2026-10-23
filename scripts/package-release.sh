#!/usr/bin/env bash
set -euo pipefail

version="${1:?Uso: ./scripts/package-release.sh VERSION}"
repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
publish_root="$repository_root/artifacts/publish/$version"
release_root="$repository_root/artifacts/release/$version"
mac_publish="$publish_root/macos-arm64"
windows_publish="$publish_root/windows-x64"
mac_app="$release_root/MatOrganizer.app"
mac_zip="$release_root/MatOrganizer-$version-macos-arm64.zip"
mac_dmg="$release_root/MatOrganizer-$version-macos-arm64.dmg"
windows_exe="$release_root/MatOrganizer-$version-windows-x64.exe"
windows_zip="$release_root/MatOrganizer-$version-windows-x64.zip"
dmg_stage="$release_root/dmg-stage"

if [[ -e "$release_root" || -e "$publish_root" ]]; then
  echo "Ya existen artefactos para la versión $version. Usa una versión nueva."
  exit 1
fi

mkdir -p "$publish_root" "$release_root"

dotnet publish "$repository_root/MatOrganizer.csproj" \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:Version="$version" \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o "$mac_publish"

mkdir -p "$mac_app/Contents/MacOS" "$mac_app/Contents/Resources"
ditto "$mac_publish" "$mac_app/Contents/MacOS"
cp "$repository_root/packaging/macos/Info.plist" "$mac_app/Contents/Info.plist"
cp "$repository_root/Assets/MatOrganizer.icns" "$mac_app/Contents/Resources/MatOrganizer.icns"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $version" "$mac_app/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $version" "$mac_app/Contents/Info.plist"
chmod +x "$mac_app/Contents/MacOS/MatOrganizer"
plutil -lint "$mac_app/Contents/Info.plist"
codesign --force --deep --sign - --timestamp=none "$mac_app"
codesign --verify --deep --strict --verbose=2 "$mac_app"

(
  cd "$release_root"
  ditto -c -k --sequesterRsrc --keepParent "$(basename "$mac_app")" "$mac_zip"
)

mkdir -p "$dmg_stage"
ditto "$mac_app" "$dmg_stage/MatOrganizer.app"
ln -s /Applications "$dmg_stage/Applications"
hdiutil create -size 256m -volname "MatOrganizer $version" -srcfolder "$dmg_stage" -ov -format UDZO "$mac_dmg"
hdiutil verify "$mac_dmg"

dotnet publish "$repository_root/MatOrganizer.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:Version="$version" \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o "$windows_publish"

cp "$windows_publish/MatOrganizer.exe" "$windows_exe"
COPYFILE_DISABLE=1 zip -j -q "$windows_zip" "$windows_exe"

echo "Artefactos creados en: $release_root"
ls -lh "$mac_dmg" "$mac_zip" "$windows_exe" "$windows_zip"
