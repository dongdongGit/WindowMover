import Cocoa
import CoreGraphics

// 保持 eventTapCallback 不变
func eventTapCallback(proxy: CGEventTapProxy, type: CGEventType, event: CGEvent, refcon: UnsafeMutableRawPointer?) -> Unmanaged<CGEvent>? {
    if type == .otherMouseDown {
        let buttonNumber = event.getIntegerValueField(.mouseEventButtonNumber)
        if buttonNumber == 2 {
            if WindowMover.shared.handleMiddleClick(event: event) {
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
    private let titleBarHeight: CGFloat = 40.0

    private init() {}

    func start() {
        print("WindowMover: Started")
        let eventMask = (1 << CGEventType.otherMouseDown.rawValue)
        guard let tap = CGEvent.tapCreate(
            tap: .cgSessionEventTap,
            place: .headInsertEventTap,
            options: .defaultTap,
            eventsOfInterest: CGEventMask(eventMask),
            callback: eventTapCallback,
            userInfo: nil
        ) else {
            print("Failed to create event tap.")
            return
        }

        self.eventTap = tap
        self.runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, tap, 0)
        CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource, .commonModes)
        CGEvent.tapEnable(tap: tap, enable: true)
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
    }

    func handleMiddleClick(event: CGEvent) -> Bool {
        guard isEnabled else { return false }
        let location = event.location
        // guard let element: AXUIElement = AccessibilityHelper.shared.getElementAtPosition(location) else { return false }
        // print("----- 开始点击诊断 -----")
        // var current: AXUIElement? = element
        // for i in 0..<5 {
        //     guard let e = current else { break }
        //     var role: CFTypeRef?
        //     var title: CFTypeRef?
        //     var desc: CFTypeRef?
        //     AXUIElementCopyAttributeValue(e, kAXRoleAttribute as CFString, &role)
        //     AXUIElementCopyAttributeValue(e, kAXTitleAttribute as CFString, &title)
        //     AXUIElementCopyAttributeValue(e, kAXDescriptionAttribute as CFString, &desc)
            
        //     print("层级 \(i): Role=\(role ?? "nil" as CFTypeRef), Title='\(title ?? "" as CFTypeRef)', Desc='\(desc ?? "" as CFTypeRef)'")
            
        //     var parent: CFTypeRef?
        //     AXUIElementCopyAttributeValue(e, kAXParentAttribute as CFString, &parent)
        //     current = (parent as! AXUIElement)
        // }
        // print("----- 诊断结束 -----")

        // [Step 1] 命中测试
        guard let element = AccessibilityHelper.shared.getElementAtPosition(location) else {
            // print("❌ Step 1 Fail: 没有获取到任何 UI 元素")
            return false
        }
        
        let role = AccessibilityHelper.shared.getElementRole(element) ?? "Unknown"
        // print("ℹ️ Hit Role: \(role)") // 调试用

        // [Step 2] 交互元素判定
        if isInteractiveTabElement(element) {
            // print("🚫 Step 2 Block: 判定为交互元素 (Tab/Button)，不移动")
            return false
        }

        // [Step 3] 查找窗口 (使用增强版查找逻辑)
        guard let window = getWindow(from: element) else {
            // print("❌ Step 3 Fail: 无法找到所属窗口 (Window Object Not Found)")
            return false
        }
        
        // [Step 4] 验证窗口 Frame
        guard let frame = AccessibilityHelper.shared.getWindowFrame(window) else {
            // print("❌ Step 4 Fail: 无法获取窗口尺寸")
            return false
        }

        // [Step 5] 区域检查
        // 如果明确点击的是 Chrome 空白条 (AXTabGroup)，则跳过坐标检查，直接移动
        if role == "AXTabGroup" {
            // print("✅ Step 5 Pass: 命中 TabGroup，强制移动")
        } else {
            if !isInTitleBar(location, frame: frame) {
                // print("🚫 Step 5 Block: 点击位置不在标题栏区域内 (Y: \(location.y), MinY: \(frame.minY))")
                return false
            }
        }

        // print("🚀 Executing Move...")
        moveWindowToNextScreen(window: window, frame: frame)
        return true
    }
    
    // --- 增强版窗口查找 ---
    // 优先使用 kAXWindowAttribute 直接获取窗口，比遍历父级更可靠
    private func getWindow(from element: AXUIElement) -> AXUIElement? {
        var window: CFTypeRef?
        
        // 尝试方法 A: 直接询问元素的 Window 属性
        let result = AXUIElementCopyAttributeValue(element, kAXWindowAttribute as CFString, &window)
        if result == .success, let w = window {
            // print("✅ Found Window via Attribute")
            return (w as! AXUIElement)
        }
        
        // 尝试方法 B: 也是备选，调用 Helper 的遍历方法 (假设 Helper 有这个方法)
        if let w = AccessibilityHelper.shared.getWindowFromElement(element) {
            // print("✅ Found Window via Hierarchy Walk")
            return w
        }
        
        return nil
    }

    private func isInteractiveTabElement(_ startElement: AXUIElement) -> Bool {
        var currentElement: AXUIElement? = startElement
        var depth = 0
        let maxDepth = 10

        while let elem = currentElement, depth < maxDepth {
            let role = AccessibilityHelper.shared.getElementRole(elem) ?? ""
            
            print("Depth: \(depth) | Role: \(role)") // 调试用

            // ----------------------------------------------------------------
            // 核心判定逻辑 (基于你提供的日志)
            // ----------------------------------------------------------------
            
            if role == "AXTabGroup" {
                if depth == 1 {
                    // 情况 1: 鼠标直接点在了 TabGroup 上 (日志中的第一种情况)
                    // -> 这就是空白区域
                    // -> 返回 false (表示不是交互元素，允许 WindowMover 移动窗口)
                    return false
                } else {
                    // 情况 2: 鼠标点在了某个子元素上，向上找父级才发现了 TabGroup (日志中的第二种情况)
                    // -> 说明点在了标签页内部 (即使那个 Group 没有标题)
                    // -> 返回 true (表示是交互元素，禁止移动)
                    return true
                }
            }

            // ----------------------------------------------------------------
            // 辅助判定 (防止直接点在文字或按钮上)
            // ----------------------------------------------------------------
            // 如果直接点到了文字(标题)、图片(图标)、按钮(关闭键)，直接拦截
            if ["AXStaticText", "AXImage", "AXButton", "AXRadioButton"].contains(role) {
                return true
            }

            // ----------------------------------------------------------------
            // 向上查找
            // ----------------------------------------------------------------
            var parent: CFTypeRef?
            let result = AXUIElementCopyAttributeValue(elem, kAXParentAttribute as CFString, &parent)
            
            if result == .success, let p = parent {
                currentElement = (p as! AXUIElement)
                depth += 1
            } else {
                break
            }
        }

        // 如果遍历完了都没遇到 TabGroup (比如点在网页内容区)，为了安全起见，不拦截
        return false
    }

    private func isInTitleBar(_ point: CGPoint, frame: CGRect) -> Bool {
        return point.y >= frame.minY && point.y <= (frame.minY + titleBarHeight) &&
               point.x >= frame.minX && point.x <= frame.maxX
    }

    private func moveWindowToNextScreen(window: AXUIElement, frame: CGRect) {
        let screens = NSScreen.screens
        // 如果只有一个屏幕，无法移动
        guard screens.count > 1 else {
            print("⚠️ Cancel Move: 只有一个显示器")
            return
        }

        let windowCenter = CGPoint(x: frame.midX, y: frame.midY)
        // 注意：NSScreen 的坐标原点在左下角，而 CGEvent/Accessibility 在左上角，需要统一
        // 这里假设 AccessibilityHelper 处理好了，或者我们使用 Frame 计算
        
        // 简单查找当前所在的 Screen (基于 Frame 中心点)
        var currentScreen: NSScreen?
        for screen in screens {
            // 将 Screen 坐标转换为 Quartz 坐标 (左上角原点) 进行比较
            let screenFrame = screen.frame
            // NSScreen 的 frame.origin.y 是基于左下角的，需要反转
            // 但 NSScreen.screens[0].frame.height 是总高度
            let globalHeight = NSScreen.screens[0].frame.height
            let quartzY = globalHeight - (screenFrame.origin.y + screenFrame.height)
            let quartzRect = CGRect(x: screenFrame.origin.x, y: quartzY, width: screenFrame.width, height: screenFrame.height)
            
            if quartzRect.contains(windowCenter) {
                currentScreen = screen
                break
            }
        }

        guard let startScreen = currentScreen ?? screens.first,
              let currentIndex = screens.firstIndex(of: startScreen) else {
            print("⚠️ Cancel Move: 无法确定当前屏幕")
            return
        }

        let nextIndex = (currentIndex + 1) % screens.count
        let nextScreen = screens[nextIndex]

        print("📺 Moving from Screen \(currentIndex) to Screen \(nextIndex)")
        
        // 计算坐标转换
        let globalHeight = NSScreen.screens[0].frame.height
        
        let startQuartzY = globalHeight - (startScreen.visibleFrame.origin.y + startScreen.visibleFrame.height)
        let startRect = CGRect(x: startScreen.visibleFrame.origin.x, y: startQuartzY, width: startScreen.visibleFrame.width, height: startScreen.visibleFrame.height)
        
        let nextQuartzY = globalHeight - (nextScreen.visibleFrame.origin.y + nextScreen.visibleFrame.height)
        let nextRect = CGRect(x: nextScreen.visibleFrame.origin.x, y: nextQuartzY, width: nextScreen.visibleFrame.width, height: nextScreen.visibleFrame.height)

        let xRatio = (frame.minX - startRect.minX) / startRect.width
        let yRatio = (frame.minY - startRect.minY) / startRect.height

        var newX = nextRect.minX + (nextRect.width * xRatio)
        var newY = nextRect.minY + (nextRect.height * yRatio)
        
        // 边界保护
        if newX + frame.width > nextRect.maxX { newX = nextRect.maxX - frame.width }
        if newY + frame.height > nextRect.maxY { newY = nextRect.maxY - frame.height }

        AccessibilityHelper.shared.setWindowPosition(window, to: CGPoint(x: newX, y: newY))
        AccessibilityHelper.shared.activateWindow(window) // 激活窗口
    }
}