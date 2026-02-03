```markdown
---
name: accessibility-permissions
description: macOS Accessibility API permission handling, checks, and user prompts for WindowMoverMac.
---

# Accessibility Permissions

## Permission Check (AccessibilityHelper.swift)
### Check Current Status
```swift
import ApplicationServices

func isAccessibilityPermissionGranted() -> Bool {
    let options: CFDictionary = [
        kAXTrustedCheckOptionPrompt.takeRetainedValue(): false
    ] as CFDictionary
    return AXIsProcessTrustedWithOptions(options)
}

func checkAndRequestPermission() -> Bool {
    if isAccessibilityPermissionGranted() {
        return true
    }

    let alert = NSAlert()
    alert.messageText = "Accessibility Access Required"
    alert.informativeText = "WindowMover needs Accessibility permissions to detect window title bars and enable drag functionality. Please grant access in System Preferences > Privacy > Accessibility."
    alert.alertStyle = .critical
    alert.addButton(withTitle: "Open System Preferences")
    alert.addButton(withTitle: "Cancel")

    if alert.runModal() == .alertFirstButtonReturn {
        openAccessibilityPreferences()
        return false
    }
    return false
}

func openAccessibilityPreferences() {
    let url = URL(string: "x-apple.systempreferences:com.apple.security.privacy_accessibility")!
    NSWorkspace.shared.open(url)
}
```

## Permission Persistence
- **First launch**: Check permission, prompt if not granted
- **Check on activate**: Verify permission still valid when app activates
- **Handle permission change**: Register for accessibility notification
- **Graceful degradation**: Show menu bar icon but disable dragging if no permission

## Login Item Helper (LoginItemHelper.swift)
### Check Login Item Status
```swift
func isLaunchAtLoginEnabled() -> Bool {
    let bundleId = Bundle.main.bundleIdentifier ?? "com.example.WindowMover"
    let jobs = SMCopyAllJobDictionaries(kSMDomainUserLaunch)
    guard let jobList = jobs?.takeRetainedValue() as? [[String: Any]] else {
        return false
    }
    return jobList.contains { $0["Label"] as? String == bundleId }
}

func setLaunchAtLogin(_ enabled: Bool) {
    let bundleId = Bundle.main.bundleIdentifier ?? "com.example.WindowMover"
    let path = Bundle.main.bundlePath

    if enabled {
        let status = SMLoginItemSetEnabled(kLoginItemIdentifier as CFString, true)
        if !status {
            print("Failed to enable launch at login")
        }
    } else {
        let jobs = SMCopyAllJobDictionaries(kSMDomainUserLaunch)
        if let jobList = jobs?.takeRetainedValue() as? [[String: Any]] {
            for job in jobList {
                if job["Label"] as? String == bundleId {
                    if let path = job["Path"] as? String {
                        let task = Process()
                        task.launchPath = "/bin/launchctl"
                        task.arguments = ["remove", bundleId]
                        task.launch()
                    }
                }
            }
        }
    }
}
```

## Permission Requirements
- **Accessibility**: Required for `AXUIElementCopyElementAtPosition`
- **Input Monitoring**: Required for `CGEventTap` on newer macOS versions
- **User Notifications**: Optional, for status alerts
- **Full Disk Access**: Not required for this app

## Testing Permission Issues
- Use `tccutil` to reset permissions during testing
- Check Console app for sandbox/permission denials
- Verify app is not in a protected path (like /Applications for sandboxed apps)
```
