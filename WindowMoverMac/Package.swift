// swift-tools-version:5.5
import PackageDescription

let package = Package(
    name: "WindowMover",
    platforms: [
        .macOS(.v10_13)
    ],
    targets: [
        .executableTarget(
            name: "WindowMover",
            dependencies: [],
            path: ".",
            exclude: ["Info.plist", "Makefile", "README.md"],
            sources: [
                "main.swift",
                "AppDelegate.swift",
                "WindowMover.swift",
                "AccessibilityHelper.swift"
            ]
        )
    ]
)