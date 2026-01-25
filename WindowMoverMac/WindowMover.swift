import Cocoa
import CoreGraphics

// Global callback function for the event tap
func eventTapCallback(proxy: CGEventTapProxy, type: CGEventType, event: CGEvent, refcon: UnsafeMutableRawPointer?) -> Unmanaged<CGEvent>? {
    if type == .otherMouseDown {
        // Middle Mouse Button Check (Button Number 2)
        let buttonNumber = event.getIntegerValueField(.mouseEventButtonNumber)
        if buttonNumber == 2 {
            if WindowMover.shared.handleMiddleClick(event: event) {
                // Swallow the event if we moved a window so it doesn't trigger other actions
                return nil
            }
        }
    }
    return Unmanaged.passUnretained(event)
}

class WindowMover {
    static let shared = WindowMover()
    
    private var eventTap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?
    public var isEnabled = true
    
    // Configurable title bar height check (approximate)
    private let titleBarHeight: CGFloat = 40.0
    
    private init() {}
    
    func start() {
        print("Starting WindowMover...")
        
        let eventMask = (1 << CGEventType.otherMouseDown.rawValue)
        
        guard let tap = CGEvent.tapCreate(
            tap: .cgSessionEventTap,
            place: .headInsertEventTap,
            options: .defaultTap,
            eventsOfInterest: CGEventMask(eventMask),
            callback: eventTapCallback,
            userInfo: nil
        ) else {
            print("Failed to create event tap. Please ensure Accessibility permissions are granted.")
            return
        }
        
        self.eventTap = tap
        self.runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, tap, 0)
        CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource, .commonModes)
        CGEvent.tapEnable(tap: tap, enable: true)
        print("Event tap started.")
    }
    
    func stop() {
        if let tap = eventTap {
            CGEvent.tapEnable(tap: tap, enable: false)
            if let source = runLoopSource {
                CFRunLoopRemoveSource(CFRunLoopGetCurrent(), source, .commonModes)
            }
            eventTap = nil
            runLoopSource = nil
        }
        print("Event tap stopped.")
    }
    
    func handleMiddleClick(event: CGEvent) -> Bool {
        guard isEnabled else { return false }
        
        let location = event.location
        
        // 1. Get window under mouse
        guard let windowElement = AccessibilityHelper.shared.getWindowUnderMouse(at: location) else {
            return false
        }
        
        // 2. Get Window Frame
        guard let frame = AccessibilityHelper.shared.getWindowFrame(windowElement) else {
            return false
        }
        
        // 3. Check if click is in title bar area (Top part of the window)
        // We allow a generous hit area for modern apps (like VS Code) that draw their own title bars
        let isTitleBar = location.y >= frame.minY && location.y <= (frame.minY + titleBarHeight) &&
                         location.x >= frame.minX && location.x <= frame.maxX
        
        if !isTitleBar {
            return false
        }
        
        // 4. Move to next screen
        moveWindowToNextScreen(window: windowElement, currentFrame: frame)
        return true
    }
    
    private func moveWindowToNextScreen(window: AXUIElement, currentFrame: CGRect) {
        let screens = NSScreen.screens
        guard screens.count > 1 else { return }
        
        // Find current screen based on window center to avoid edge cases
        let windowCenter = CGPoint(x: currentFrame.midX, y: currentFrame.midY)
        
        // Find screen containing the center point
        // Note: NSScreen coordinates have origin at bottom-left, but CoreGraphics/AX use top-left (usually).
        // However, NSScreen.frame is in Cocoa coordinates. AX uses Quartz coordinates.
        // We need to be careful. accessibilityFrame() usually returns Quartz coords (top-left 0,0).
        // NSScreen 0,0 is bottom-left of primary screen.
        
        // Helper to find screen for a Quartz point
        var currentScreen: NSScreen? = nil
        for screen in screens {
            // Convert Quartz point to Cocoa point for hit testing
            // Quartz y = DisplayHeight - Cocoa y
            // Actually simpler: just check geometry relative to screen frame in global coords
            // AX coordinates are global display coordinates (Top-Left origin of main screen).
            
            // Let's use direct comparison logic assuming screens are arranged logically
            // A robust way is to just find which screen frame contains the point.
            // NSScreen frames are strict. We need to convert point.
            
            // Actually, let's use a simpler heuristic: Iterating screens and checking bounds logic match
            // But since coordinates systems differ (Y-axis flip), we need to flip Y.
            
            let screenFrame = screen.frame
            // Flip Y for check. Global height needed? No, just relative to primary screen.
            // Primary screen is index 0. Its bottom-left is (0,0) in Cocoa.
            // In Quartz, (0,0) is top-left of primary screen.
            
            // Let's rely on standard center-point check
            // Convert Cocoa Frame to Quartz Frame roughly
            let globalHeight = NSScreen.screens[0].frame.height
            let quartzY = globalHeight - (screenFrame.origin.y + screenFrame.height)
            let quartzFrame = CGRect(x: screenFrame.origin.x, y: quartzY, width: screenFrame.width, height: screenFrame.height)
            
            if quartzFrame.contains(windowCenter) {
                currentScreen = screen
                break
            }
        }
        
        // Fallback to first screen if not found
        guard let startScreen = currentScreen ?? screens.first else { return }
        guard let currentIndex = screens.firstIndex(of: startScreen) else { return }
        
        let nextIndex = (currentIndex + 1) % screens.count
        let nextScreen = screens[nextIndex]
        
        // Logic: Calculate relative position (0.0 - 1.0) on current screen
        // We need Quartz frames for calculation
        let globalHeight = NSScreen.screens[0].frame.height
        
        let currentQuartzY = globalHeight - (startScreen.visibleFrame.origin.y + startScreen.visibleFrame.height)
        let currentQuartzRect = CGRect(x: startScreen.visibleFrame.origin.x, y: currentQuartzY, width: startScreen.visibleFrame.width, height: startScreen.visibleFrame.height)
        
        let nextQuartzY = globalHeight - (nextScreen.visibleFrame.origin.y + nextScreen.visibleFrame.height)
        let nextQuartzRect = CGRect(x: nextScreen.visibleFrame.origin.x, y: nextQuartzY, width: nextScreen.visibleFrame.width, height: nextScreen.visibleFrame.height)
        
        let xRatio = (currentFrame.minX - currentQuartzRect.minX) / currentQuartzRect.width
        let yRatio = (currentFrame.minY - currentQuartzRect.minY) / currentQuartzRect.height
        
        // Calculate new position
        var newX = nextQuartzRect.minX + (nextQuartzRect.width * xRatio)
        var newY = nextQuartzRect.minY + (nextQuartzRect.height * yRatio)
        
        // Resize if needed (if next screen is smaller)
        let newWidth = min(currentFrame.width, nextQuartzRect.width)
        let newHeight = min(currentFrame.height, nextQuartzRect.height)
        
        // Clamp to bounds to ensure window is visible
        if newX + newWidth > nextQuartzRect.maxX {
            newX = nextQuartzRect.maxX - newWidth
        }
        if newY + newHeight > nextQuartzRect.maxY {
            newY = nextQuartzRect.maxY - newHeight
        }
        
        // Apply
        let newOrigin = CGPoint(x: newX, y: newY)
        let newSize = CGSize(width: newWidth, height: newHeight)
        
        AccessibilityHelper.shared.setWindowPosition(window, to: newOrigin)
        AccessibilityHelper.shared.setWindowSize(window, to: newSize)
        AccessibilityHelper.shared.raiseWindow(window)
    }
}
