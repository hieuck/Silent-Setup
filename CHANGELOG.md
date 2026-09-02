# Changelog

All notable changes to Silent Setup will be documented in this file.

## [1.0.0] - 2026-09-01

### Initial Release

#### Features
- ✅ **Automated Silent Installation**: Install multiple applications without user interaction
- ✅ **Smart Detection**: Automatically detect installed applications via registry and file system
- ✅ **Patch & Plugin Support**: Vietnamese localization, plugins, and custom patches
- ✅ **Portable Application**: No installation required, runs directly from executable
- ✅ **Easy Extension**: Add new apps through UI or YAML configuration files
- ✅ **Search & Filter**: Search by name, category, publisher with advanced filtering
- ✅ **In-App Management**: Add, edit, delete apps through graphical interface
- ✅ **Download Management**: HTTP download with caching, resume capability, SHA256 verification
- ✅ **Multiple Installer Types**: Support for EXE, MSI, and ZIP/portable installers
- ✅ **Progress Tracking**: Real-time progress bar and status updates
- ✅ **Logging System**: Detailed logs for troubleshooting

#### Built-in Apps (7 total)
- Google Chrome (Browser)
- Mozilla Firefox (Browser)
- Visual Studio Code (Development)
- Notepad++ (Development)
- VLC Media Player (Media)
- 7-Zip (Utility)

#### Technical Stack
- .NET 8.0 with WPF
- YamlDotNet for manifest parsing
- Newtonsoft.Json for configuration
- Single-file self-contained executable (155 MB)

#### Documentation
- Complete user guide in Vietnamese
- App manifest specification
- Patch manifest specification
- Architecture documentation
- README with quick start guide

#### Known Limitations
- Windows 10/11 64-bit only
- Requires administrator rights for most installations
- Some antivirus software may flag the executable (false positive)
- Download links must use HTTPS

#### Files
- `SilentSetup-v1.0-win64.zip` (62.97 MB)
- SHA256: `AD6C3CF110F6DB010E957AF5A2126BD20065200677D3FA6A83224544589BB261`

---

## Roadmap

### Planned for v1.1
- [ ] Auto-update mechanism
- [ ] Batch profile system (install presets)
- [ ] Update checker for installed apps
- [ ] More built-in app manifests
- [ ] Settings dialog implementation
- [ ] Multi-language UI support

### Future Considerations
- [ ] Plugin system for custom installers
- [ ] Cloud sync for configurations
- [ ] Scheduled installations
- [ ] Rollback/uninstall support
- [ ] Package verification with digital signatures
