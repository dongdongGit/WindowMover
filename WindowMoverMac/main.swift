import Cocoa

// Initialize the application
let app = NSApplication.shared

// Create and assign the delegate
let delegate = AppDelegate()
app.delegate = delegate

// Start the event loop
app.run()
