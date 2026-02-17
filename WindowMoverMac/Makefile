APP_NAME = WindowMover
SOURCES = main.swift AppDelegate.swift WindowMover.swift AccessibilityHelper.swift LoginItemHelper.swift

all: bundle

build:
	swiftc -o $(APP_NAME) $(SOURCES) -framework Cocoa -framework Carbon

# 针对特定架构编译
build-arm64:
	swiftc -target arm64-apple-macosx10.13 -o $(APP_NAME)-arm64 $(SOURCES) -framework Cocoa -framework Carbon

build-x86_64:
	swiftc -target x86_64-apple-macosx10.13 -o $(APP_NAME)-x86_64 $(SOURCES) -framework Cocoa -framework Carbon

# 通用二进制（Universal Binary）
build-universal: build-arm64 build-x86_64
	lipo -create -output $(APP_NAME) $(APP_NAME)-arm64 $(APP_NAME)-x86_64
	rm -f $(APP_NAME)-arm64 $(APP_NAME)-x86_64

bundle: build
	mkdir -p $(APP_NAME).app/Contents/MacOS
	mkdir -p $(APP_NAME).app/Contents/Resources
	cp Info.plist $(APP_NAME).app/Contents/
	cp $(APP_NAME) $(APP_NAME).app/Contents/MacOS/
	@echo "App Bundle created at $(APP_NAME).app"

# 使用特定架构打包
bundle-arch:
	mkdir -p $(APP_NAME).app/Contents/MacOS
	mkdir -p $(APP_NAME).app/Contents/Resources
	cp Info.plist $(APP_NAME).app/Contents/
	cp $(APP_NAME) $(APP_NAME).app/Contents/MacOS/
	@echo "App Bundle created at $(APP_NAME).app"

# 按架构打包：ARM64 (Apple Silicon)
bundle-arm64: build-arm64
	cp $(APP_NAME)-arm64 $(APP_NAME)
	rm -f $(APP_NAME)-arm64
	$(MAKE) bundle-arch

# 按架构打包：x86_64 (Intel)
bundle-x86_64: build-x86_64
	cp $(APP_NAME)-x86_64 $(APP_NAME)
	rm -f $(APP_NAME)-x86_64
	$(MAKE) bundle-arch

# 通用版打包
bundle-universal: build-universal
	$(MAKE) bundle-arch

clean:
	rm -f $(APP_NAME) $(APP_NAME)-arm64 $(APP_NAME)-x86_64
	rm -rf $(APP_NAME).app