# PaneLock

**Make better use of an ultrawide monitor, a multi-monitor desk, or a CORSAIR XENEON EDGE.**

PaneLock is a Windows 11 window manager that divides each display into custom panes and keeps assigned apps in place. Its Chrome companion makes YouTube's fullscreen button fill an assigned pane and hide the browser controls, so a video can sit alongside chat, notes, or another app.

[Download the latest release](https://github.com/carlosnewideaweb/PaneLock/releases/latest) · [Report a bug](https://github.com/carlosnewideaweb/PaneLock/issues) · [MIT License](LICENSE)

## Built for wide workspaces

- **Ultrawide and super-ultrawide monitors:** use a large center pane for a browser or video while keeping other apps visible on the sides.
- **Multi-monitor setups:** choose independent layouts on every display, move assigned windows between screens, and retain preferred monitor assignments through disconnects.
- **CORSAIR XENEON EDGE:** turn the companion display into a workspace for video, chat, or utility apps. The [XENEON EDGE's 2560 × 720 format](https://www.corsair.com/us/en/p/monitors/cc-9011306-ww/xeneon-edge-14-5-lcd-touchscreen-cc-9011306-ww) suits horizontal pane layouts. PaneLock uses it as an ordinary extended Windows display; it does not provide iCUE integration or custom touch gestures.

An example on a 5120-pixel-wide display:

| Left app | Center browser / video | Right app |
| --- | --- | --- |
| 1600 px | **1920 px** | 1600 px |

## Features

- One, two, or three panes per monitor, with draggable dividers.
- Exact middle-pane width in pixels, optional equal sides, and independent left width.
- **Shift + title-bar drag** to highlight and snap into predefined panes.
- App window list, drag-to-assign preview, and keyboard shortcuts.
- Saved layouts and restoration of matching app windows.
- Pane-contained Chrome F11 mode and one-click YouTube pane fullscreen.
- Per-monitor taskbar space and support for different display scaling.
- Recovery helper that restores placement and clipping after an unexpected app exit.
- Tray controls, reopening an existing instance, per-user installation, and optional startup with Windows.

> PaneLock emulates fullscreen within a window region. It does **not** turn panes into real Windows monitors. Chrome gets specific fullscreen support; other apps support assignment and maximize confinement, while their own native or exclusive fullscreen behavior varies.

## Install

Requires **Windows 11 x64** and **.NET Framework 4.8**. Google Chrome is required for the YouTube companion.

1. Download `PaneLock-Setup.exe` from [Releases](https://github.com/carlosnewideaweb/PaneLock/releases/latest).
2. If upgrading, choose **Exit** from the old PaneLock tray menu first.
3. Run the installer, then open PaneLock from the Start menu. It installs for your account without administrator access.
4. Alternatively, extract the portable ZIP and run `PaneLock.exe`. Keep `PaneLock.exe.config` beside it.

The current installer is not code-signed. The source and build instructions are available below.

### Enable YouTube pane fullscreen

1. Open `chrome://extensions` in Chrome and enable **Developer mode**.
2. Choose **Load unpacked** and select the `YouTubeCompanion` folder containing `manifest.json`. The installed copy is at `%LocalAppData%\PaneLock\YouTubeCompanion`; the portable ZIP includes the same folder.
3. Use companion version **0.3.1**. Remove duplicate older copies, or click **Reload** after updating an existing unpacked copy.
4. **Reload your YouTube tab.** Existing tabs must be refreshed after an extension update.
5. Assign the actual Chrome window containing YouTube to a pane, then click the player's fullscreen button.

The player fills that pane and Chrome's tabs, address bar, and bookmarks hide automatically. **Escape** or the player exit button restores them. If you enabled F11 mode beforehand, leaving video fullscreen preserves that earlier mode.

If fullscreen escapes, check that the YouTube window—not a different Chrome window—is assigned in PaneLock, the companion has access to YouTube, and the tab has been reloaded.

## Set up your workspace

Select a monitor from the map or dropdown, choose its pane count, and drag dividers or enter a middle width. Choose **Apply size** after typing a width. Keep **Equal side panes** checked to share the remainder, or uncheck it and enter the left width; the right side fills what remains.

Assign an app using the window list and pane buttons, or hold **Shift** while dragging its title bar and release the mouse over a highlighted pane. Widths are physical pixels at the current resolution and are saved as proportions. Chrome's minimum width limits how many panes fit. Equal sides can differ by one pixel on odd totals.

| Shortcut | Action |
| --- | --- |
| Ctrl + Alt + 1 / 2 / 3 | Assign focused app to a pane |
| Ctrl + Alt + Left / Right | Move assigned app between monitors |
| Shift + title-bar drag | Highlight panes and snap on drop |
| F11 | Toggle pane fullscreen for assigned Chrome |
| Ctrl + Alt + 0 | Unlock focused window |
| Ctrl + Alt + Esc | Unlock all and clear remembered assignments |

Closing the settings window leaves PaneLock in the tray. Launch it again or left-click its tray icon to reopen it. Choose **Exit** from the tray menu to stop it.

## Restoration and limits

Settings and recovery records live in `%LocalAppData%\PaneLock`; they are not part of this repository. Still-open windows restore by handle, process ID, and process start time. Restarted apps require one uniquely matching title and process name. PaneLock does not reopen apps or tabs. Changed titles or monitor device identifiers can require reassignment.

The recovery helper restores placement and clipping after a crash. If both processes are stopped, recovery is attempted on the next launch. Run PaneLock and managed apps with matching permissions. Some apps impose larger minimum sizes or reject resizing.

Games, exclusive fullscreen, separate popup windows, and Chrome's menu-driven native fullscreen are outside the supported fullscreen path. The companion works on `www.youtube.com` and `m.youtube.com`, not players embedded on other sites. A temporary tab-title marker coordinates toolbar visibility with the desktop app; no local server or external API is involved.

## Build from source

No .NET SDK or third-party package download is needed for the desktop build. Run in PowerShell on Windows:

```powershell
.\src\Build.ps1
```

The build uses Windows' .NET Framework compiler and produces `PaneLock.exe`, `PaneLock.exe.config`, `PaneLock-Setup.exe`, and `PaneLock.ico` in the repository root. Include `YouTubeCompanion` in portable distributions; the installer embeds it.

## Test

```powershell
$testRoot = Join-Path $env:TEMP 'PaneLock-tests'
$test = Start-Process .\PaneLock.exe -ArgumentList @('--self-test', '--test-root', $testRoot) -Wait -PassThru
Get-Content (Join-Path $testRoot 'reports\unit-tests.txt')
if ($test.ExitCode -ne 0) { throw 'Self-tests failed' }
```

Optional `--desktop-test --test-root <scratch-directory>` tests open an isolated Chrome profile and move its test window between monitors. Run them in an interactive Windows session; set `PANELOCK_CHROME_PATH` if Chrome is installed somewhere unusual.

Optional companion tests require Node.js and Playwright:

```powershell
npm install
npx playwright install chromium
npm test
```

Tests cover geometry, exact widths, saved settings, generic app placement, fullscreen routing, restoration, and player behavior. Automated drag checks simulate Shift; physical modifier handling and additional hardware combinations benefit from hands-on testing.

## Contributing

Issues and pull requests are welcome. Include Windows/app/companion versions, resolution/scaling, pane layout, and reproduction steps. Do not post credentials, browser profiles, or full settings/recovery files.

## License and affiliation

[MIT](LICENSE). Copyright © 2026 carlosnewideaweb.

PaneLock is an independent project, not affiliated with or endorsed by Microsoft, Google, or CORSAIR. Product names belong to their respective owners.
