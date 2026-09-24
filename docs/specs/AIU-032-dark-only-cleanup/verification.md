# AIU-032 verification

Date: 2026-09-24. Windows 11 Pro 10.0.26200 x64, .NET SDK 10.0.401 (global.json), display
scale 125 %. Base `b93b42e`; implementation `decb204` (theme removal) and `32f8b67` (removed-UI
leftovers); completed in the commit that adds this record. No other AiUsage instance was
running; every agent-launched instance was a separate unpackaged process with an isolated state
directory under the session scratch folder, and each was closed afterwards.

## Results by acceptance criterion

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS, partly NOT_RUN | The application requests the dark theme and `Tokens.xaml` holds one dark dictionary, so no code path reads the Windows app mode. Captures of the usage view, Settings, account detail and the Exit dialog are dark. NOT_RUN: a capture with Windows switched to light app mode; the host was in dark mode and its personalization setting was not changed. |
| AC-02 | PASS | Smoke row `appearance` (demo and product) opens Settings and asserts that `ThemeOptions`, `ThemeNote`, `ThemeSystem`, `ThemeLight`, `ThemeDark`, `DemoWindowsMode` and `DemoHighContrast` are absent; the capture shows Layout and Order sections only. `ThemePreference`, `EffectiveTheme`, `IThemeService`, `PreferenceKey.Theme`, the theme strings and `SimulatedHighContrast.xaml` are deleted. |
| AC-03 | PASS | `LiveAdapterTests.PreferenceFileWithUnknownMembersRoundTripsUnchanged` loads a file with `"Theme":2`, keeps the other preferences and writes the theme value back unchanged. The upgrade and feed-update harnesses now seed `"AlwaysOnTop":true` beside the old theme value and assert the always-on-top switch. |
| AC-04 | PASS | Deleted: the local History view model and its chart control, chart markers and axis ticks, the Overview variants of the window line, `ArrowNavigation`, unused bind helpers, six unused styles, the NavigationView and two unused token brushes, and 61 unused strings. A reference scan of strings, styles and public members reports only the dynamically built `RecoveryLive_Body_*` keys. |
| AC-05 | PASS | Presentation 167/167 (five tests of the deleted History page removed, gap and reset chart semantics kept), Infrastructure 330/330, ProjectValidation tests 80/80, documents valid. Windows smoke rows launch, navigation, appearance, tray-exit, repeated-exit and capabilities: demo 6/6 and empty isolated product state 6/6. Unsigned MSIX `2026.9.2453.0` built (SHA-256 `CE6ACD8511E76504417799B4149680F9FA3D008870E97DC975FD79D2185AF69B`), not installed. |

## Observations

- The smoke capture `view-account.png` is blank in this and in earlier runs (same file size); it
  is taken before the detail page paints. An interactive capture of Research's detail showed
  the page, the sparkline and the detail window lines correctly.

## Not run

- NOT_RUN: rendering with Windows in light app mode or with a Windows contrast theme active.
- NOT_RUN: packaged install, upgrade and feed-update harnesses (their preference assertion
  changed from the theme note to the always-on-top switch).
