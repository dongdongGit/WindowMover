import Cocoa
import ApplicationServices

class AccessibilityHelper {
    static let shared = AccessibilityHelper()
    
    private init() {}
    
    func checkAccessibilityPermissions() -> Bool {
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        return AXIsProcessTrustedWithOptions(options)
    }
    
    func getWindowUnderMouse(at point: CGPoint) -> AXUIElement? {
        let systemWide = AXUIElementCreateSystemWide()
        var element: AXUIElement?
        
        let result = AXUIElementCopyElementAtPosition(systemWide, Float(point.x), Float(point.y), &element)
        
        if result == .success, let element = element {
            // We might have hit a button or title bar inside the window, need to find the actual Window element
            return getWindowFromElement(element)
        }
        
        return nil
    }
    
    private func getWindowFromElement(_ element: AXUIElement) -> AXUIElement? {
        var currentElement = element
        
        while true {
            var role: AnyObject?
            let roleResult = AXUIElementCopyAttributeValue(currentElement, kAXRoleAttribute as CFString, &role)
            
            if roleResult == .success, let roleStr = role as? String {
                if roleStr == kAXWindowRole as String {
                    return currentElement
                }
            }
            
            var parent: AnyObject?
            let parentResult = AXUIElementCopyAttributeValue(currentElement, kAXParentAttribute as CFString, &parent)
            
            if parentResult == .success, let parentElement = parent {
                // Create AXUIElement from the raw object (CFTypeRef)
                currentElement = parentElement as! AXUIElement
            } else {
                break
            }
        }
        
        return nil
    }
    
    func getWindowFrame(_ window: AXUIElement) -> CGRect? {
        var positionValue: AnyObject?
        var sizeValue: AnyObject?
        
        let posResult = AXUIElementCopyAttributeValue(window, kAXPositionAttribute as CFString, &positionValue)
        let sizeResult = AXUIElementCopyAttributeValue(window, kAXSizeAttribute as CFString, &sizeValue)
        
        if posResult == .success, sizeResult == .success {
            var point = CGPoint.zero
            var size = CGSize.zero
            
            AXValueGetValue(positionValue as! AXValue, .cgPoint, &point)
            AXValueGetValue(sizeValue as! AXValue, .cgSize, &size)
            
            return CGRect(origin: point, size: size)
        }
        
        return nil
    }
    
    func setWindowPosition(_ window: AXUIElement, to point: CGPoint) {
        var newPoint = point
        if let value = AXValueCreate(.cgPoint, &newPoint) {
            AXUIElementSetAttributeValue(window, kAXPositionAttribute as CFString, value)
        }
    }
    
    func setWindowSize(_ window: AXUIElement, to size: CGSize) {
        var newSize = size
        if let value = AXValueCreate(.cgSize, &newSize) {
            AXUIElementSetAttributeValue(window, kAXSizeAttribute as CFString, value)
        }
    }
    
    func raiseWindow(_ window: AXUIElement) {
         AXUIElementSetAttributeValue(window, kAXMainAttribute as CFString, true as CFTypeRef)
         AXUIElementPerformAction(window, kAXRaiseAction as CFString)
    }
}
