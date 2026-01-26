APP_NAME = WindowMover
SOURCES = main.swift AppDelegate.swift WindowMover.swift AccessibilityHelper.swift LoginItemHelper.swift

all: bundle

build:
	swiftc -o $(APP_NAME) $(SOURCES) -framework Cocoa -framework Carbon

bundle: build
	mkdir -p $(APP_NAME).app/Contents/MacOS
	mkdir -p $(APP_NAME).app/Contents/Resources
	cp Info.plist $(APP_NAME).app/Contents/
	cp $(APP_NAME) $(APP_NAME).app/Contents/MacOS/
	@echo "App Bundle created at $(APP_NAME).app"

clean:
	rm -f $(APP_NAME)
	rm -rf $(APP_NAME).app