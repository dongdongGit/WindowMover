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
    
    func getChildren(of element: AXUIElement) -> [AXUIElement]? {
        var childrenValue: AnyObject?
        let result = AXUIElementCopyAttributeValue(element, kAXChildrenAttribute as CFString, &childrenValue)
        
        if result == .success, let childrenArray = childrenValue as? [AXUIElement] {
            return childrenArray
        }
        
        return nil
    }
    
    func getElementFrame(_ element: AXUIElement) -> CGRect? {
        var positionValue: AnyObject?
        var sizeValue: AnyObject?
        
        let posResult = AXUIElementCopyAttributeValue(element, kAXPositionAttribute as CFString, &positionValue)
        let sizeResult = AXUIElementCopyAttributeValue(element, kAXSizeAttribute as CFString, &sizeValue)
        
        if posResult == .success, sizeResult == .success {
            var point = CGPoint.zero
            var size = CGSize.zero
            
            AXValueGetValue(positionValue as! AXValue, .cgPoint, &point)
            AXValueGetValue(sizeValue as! AXValue, .cgSize, &size)
            
            return CGRect(origin: point, size: size)
        }
        
        return nil
    }
    
    func getElementRole(_ element: AXUIElement) -> String? {
        var roleValue: AnyObject?
        let result = AXUIElementCopyAttributeValue(element, kAXRoleAttribute as CFString, &roleValue)
        
        if result == .success, let role = roleValue as? String {
            return role
        }
        
        return nil
    }
    
    func isWindowMain(_ window: AXUIElement) -> Bool {
        var isMainValue: AnyObject?
        let result = AXUIElementCopyAttributeValue(window, kAXMainAttribute as CFString, &isMainValue)
        
        if result == .success, let isMain = isMainValue as? Bool {
            return isMain
        }
        
        return false
    }
    
    func isWindowFocused(_ window: AXUIElement) -> Bool {
        var isFocusedValue: AnyObject?
        let result = AXUIElementCopyAttributeValue(window, kAXFocusedAttribute as CFString, &isFocusedValue)
        
        if result == .success, let isFocused = isFocusedValue as? Bool {
            return isFocused
        }
        
        return false
    }
    
    func focusWindow(_ window: AXUIElement) {
        AXUIElementSetAttributeValue(window, kAXMainAttribute as CFString, true as CFTypeRef)
        AXUIElementSetAttributeValue(window, kAXFocusedAttribute as CFString, true as CFTypeRef)
    }
}
