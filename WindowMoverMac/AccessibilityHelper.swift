import Cocoa
import ApplicationServices

class AccessibilityHelper {
    static let shared = AccessibilityHelper()

    private init() {}

    func checkAccessibilityPermissions() -> Bool {
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        return AXIsProcessTrustedWithOptions(options)
    }

    func getElementAtPosition(_ point: CGPoint) -> AXUIElement? {
        let systemWide = AXUIElementCreateSystemWide()
        var element: AXUIElement?

        let result = AXUIElementCopyElementAtPosition(systemWide, Float(point.x), Float(point.y), &element)

        if result == .success, let el = element {
            return el
        }

        return nil
    }

    func getWindowFromElement(_ element: AXUIElement) -> AXUIElement? {
        var currentElement: AXUIElement? = element

        while let elem = currentElement {
            let role = getElementRole(elem)
            if role == kAXWindowRole as String || role == "AXStandardWindow" {
                return elem
            }

            var parent: CFTypeRef?
            let parentResult = AXUIElementCopyAttributeValue(elem, kAXParentAttribute as CFString, &parent)

            if parentResult == .success {
                currentElement = (parent as! AXUIElement)
            } else {
                break
            }
        }

        return nil
    }

    func getWindowFrame(_ window: AXUIElement) -> CGRect? {
        var positionValue: CFTypeRef?
        var sizeValue: CFTypeRef?

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

    func activateWindow(_ window: AXUIElement) {
        var pid: pid_t = 0
        AXUIElementGetPid(window, &pid)

        if let app = NSRunningApplication(processIdentifier: pid) {
            app.activate(options: [.activateAllWindows])
        }

        AXUIElementSetAttributeValue(window, kAXMainAttribute as CFString, true as CFTypeRef)
        AXUIElementPerformAction(window, kAXRaiseAction as CFString)
    }

    func getElementRole(_ element: AXUIElement) -> String? {
        var roleValue: CFTypeRef?
        let result = AXUIElementCopyAttributeValue(element, kAXRoleAttribute as CFString, &roleValue)

        if result == .success, let role = roleValue as? String {
            return role
        }

        return nil
    }

    func getElementSubrole(_ element: AXUIElement) -> String? {
        var subroleValue: CFTypeRef?
        let result = AXUIElementCopyAttributeValue(element, kAXSubroleAttribute as CFString, &subroleValue)

        if result == .success, let subrole = subroleValue as? String {
            return subrole
        }

        return nil
    }

    func getWindowTitle(_ window: AXUIElement) -> String? {
        var titleValue: CFTypeRef?
        let result = AXUIElementCopyAttributeValue(window, kAXTitleAttribute as CFString, &titleValue)

        if result == .success, let title = titleValue as? String {
            return title
        }

        return nil
    }

    func getChildren(of element: AXUIElement) -> [AXUIElement]? {
        var childrenValue: CFTypeRef?
        let result = AXUIElementCopyAttributeValue(element, kAXChildrenAttribute as CFString, &childrenValue)

        if result == .success, let childrenArray = childrenValue as? [AXUIElement] {
            return childrenArray
        }

        return nil
    }

    func getElementFrame(_ element: AXUIElement) -> CGRect? {
        var positionValue: CFTypeRef?
        var sizeValue: CFTypeRef?

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
}
