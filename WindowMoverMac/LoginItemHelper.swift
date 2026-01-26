import Cocoa
import ServiceManagement

class LoginItemHelper {
    static let shared = LoginItemHelper()
    
    private init() {}
    
    func isLoginItemEnabled() -> Bool {
        if #available(macOS 13.0, *) {
            return SMAppService.mainApp.status == .enabled
        } else {
            // Legacy approach using SMLoginItemSetEnabled for macOS < 13.0
            guard let bundleIdentifier = Bundle.main.bundleIdentifier else { return false }
            return SMLoginItemSetEnabled(bundleIdentifier as CFString, true)
        }
    }
    
    func setLoginItemEnabled(_ enabled: Bool) {
        if #available(macOS 13.0, *) {
            do {
                if enabled {
                    try SMAppService.mainApp.register()
                } else {
                    try SMAppService.mainApp.unregister()
                }
            } catch {
                print("Failed to \(enabled ? "enable" : "disable") login item: \(error)")
            }
        } else {
            // Legacy approach for older macOS versions
            guard let bundleIdentifier = Bundle.main.bundleIdentifier else { return }
            let success = SMLoginItemSetEnabled(bundleIdentifier as CFString, enabled)
            if !success {
                print("Failed to \(enabled ? "enable" : "disable") login item using legacy API")
            }
        }
    }
}