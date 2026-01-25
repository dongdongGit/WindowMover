import Cocoa

class AppDelegate: NSObject, NSApplicationDelegate {
    var statusItem: NSStatusItem!
    
    func applicationDidFinishLaunching(_ aNotification: Notification) {
        // 1. Create Status Bar Item
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        if let button = statusItem.button {
            // Simple ASCII arrow icon for now, or use a system symbol
            if #available(macOS 11.0, *) {
                button.image = NSImage(systemSymbolName: "arrow.left.and.right.circle", accessibilityDescription: "Window Mover")
            } else {
                button.title = "WM"
            }
        }
        
        // 2. Setup Menu
        constructMenu()
        
        // 3. Check Permissions
        checkPermissions()
        
        // 4. Start Core Logic
        WindowMover.shared.start()
    }
    
    func constructMenu() {
        let menu = NSMenu()
        
        menu.addItem(NSMenuItem(title: "Window Mover is Running", action: nil, keyEquivalent: ""))
        menu.addItem(NSMenuItem.separator())
        
        let quitItem = NSMenuItem(title: "Quit", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        menu.addItem(quitItem)
        
        statusItem.menu = menu
    }
    
    func checkPermissions() {
        if !AccessibilityHelper.shared.checkAccessibilityPermissions() {
            let alert = NSAlert()
            alert.messageText = "Accessibility Permission Required"
            alert.informativeText = "Window Mover needs accessibility permissions to move windows. Please grant access in System Settings -> Privacy & Security -> Accessibility."
            alert.alertStyle = .warning
            alert.addButton(withTitle: "Open Settings")
            alert.addButton(withTitle: "Quit")
            
            let response = alert.runModal()
            if response == .alertFirstButtonReturn {
                // Open Accessibility Settings
                if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility") {
                    NSWorkspace.shared.open(url)
                }
            } else {
                NSApplication.shared.terminate(nil)
            }
        }
    }
    
    func applicationWillTerminate(_ aNotification: Notification) {
        WindowMover.shared.stop()
    }
}
