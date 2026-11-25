package main

import (
	"embed"
	"encoding/json"
	"fmt"
	"net"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"syscall"
	"time"
	"unsafe"
)

//go:embed ui/*
var uiFS embed.FS

// Win32 API Function Pointers
var (
	kernel32 = syscall.NewLazyDLL("kernel32.dll")
	user32   = syscall.NewLazyDLL("user32.dll")
	shell32  = syscall.NewLazyDLL("shell32.dll")
	winmm    = syscall.NewLazyDLL("winmm.dll")
	comdlg32 = syscall.NewLazyDLL("comdlg32.dll")
	advapi32 = syscall.NewLazyDLL("advapi32.dll")

	procGetModuleHandleW      = kernel32.NewProc("GetModuleHandleW")
	procRegisterClassExW     = user32.NewProc("RegisterClassExW")
	procCreateWindowExW       = user32.NewProc("CreateWindowExW")
	procDefWindowProcW        = user32.NewProc("DefWindowProcW")
	procDestroyWindow         = user32.NewProc("DestroyWindow")
	procGetMessageW           = user32.NewProc("GetMessageW")
	procTranslateMessage      = user32.NewProc("TranslateMessage")
	procDispatchMessageW      = user32.NewProc("DispatchMessageW")
	procPostQuitMessage       = user32.NewProc("PostQuitMessage")
	procFindWindowW           = user32.NewProc("FindWindowW")
	procFindWindowExW         = user32.NewProc("FindWindowExW")
	procSendMessageTimeoutW   = user32.NewProc("SendMessageTimeoutW")
	procEnumWindows           = user32.NewProc("EnumWindows")
	procSetParent             = user32.NewProc("SetParent")
	procSetWindowLongPtrW     = user32.NewProc("SetWindowLongPtrW")
	procSetWindowPos          = user32.NewProc("SetWindowPos")
	procGetSystemMetrics      = user32.NewProc("GetSystemMetrics")
	procGetForegroundWindow   = user32.NewProc("GetForegroundWindow")
	procGetWindowRect         = user32.NewProc("GetWindowRect")
	procGetClassNameW         = user32.NewProc("GetClassNameW")
	procMonitorFromWindow     = user32.NewProc("MonitorFromWindow")
	procGetMonitorInfoW       = user32.NewProc("GetMonitorInfoW")
	procIsIconic              = user32.NewProc("IsIconic")
	procCreatePopupMenu       = user32.NewProc("CreatePopupMenu")
	procAppendMenuW           = user32.NewProc("AppendMenuW")
	procTrackPopupMenuEx      = user32.NewProc("TrackPopupMenuEx")
	procDestroyMenu           = user32.NewProc("DestroyMenu")
	procSetForegroundWindow   = user32.NewProc("SetForegroundWindow")
	procGetCursorPos          = user32.NewProc("GetCursorPos")
	procMessageBoxW           = user32.NewProc("MessageBoxW")
	procSystemParametersInfoW = user32.NewProc("SystemParametersInfoW")

	procShell_NotifyIconW     = shell32.NewProc("Shell_NotifyIconW")
	procExtractIconExW        = shell32.NewProc("ExtractIconExW")

	procMciSendStringW        = winmm.NewProc("mciSendStringW")

	procGetOpenFileNameW      = comdlg32.NewProc("GetOpenFileNameW")

	procRegOpenKeyExW         = advapi32.NewProc("RegOpenKeyExW")
	procRegSetValueExW        = advapi32.NewProc("RegSetValueExW")
	procRegDeleteValueW       = advapi32.NewProc("RegDeleteValueW")
	procRegCloseKey           = advapi32.NewProc("RegCloseKey")
)

const (
	WS_CHILD          = 0x40000000
	WS_VISIBLE        = 0x10000000
	WS_CLIPSIBLINGS   = 0x04000000
	WS_CLIPCHILDREN   = 0x02000000
	WS_EX_TOOLWINDOW  = 0x00000080
	WS_EX_TRANSPARENT = 0x00000020
	WS_EX_NOACTIVATE  = 0x08000000

	SWP_NOSIZE        = 0x0001
	SWP_NOMOVE        = 0x0002
	SWP_NOZORDER      = 0x0004
	SWP_NOACTIVATE    = 0x0010
	SWP_FRAMECHANGED  = 0x0020
	SWP_SHOWWINDOW    = 0x0040

	SM_CXSCREEN        = 0
	SM_CYSCREEN        = 1
	SM_CXVIRTUALSCREEN = 78
	SM_CYVIRTUALSCREEN = 79
	SM_XVIRTUALSCREEN  = 76
	SM_YVIRTUALSCREEN  = 77

	WM_USER          = 0x0400
	WM_TRAYICON      = WM_USER + 1
	WM_COMMAND       = 0x0111
	WM_RBUTTONUP     = 0x0205
	WM_LBUTTONUP     = 0x0202
	WM_LBUTTONDBLCLK = 0x0203
	WM_DESTROY       = 0x0002

	NIM_ADD          = 0x00000000
	NIM_DELETE       = 0x00000002
	NIF_MESSAGE      = 0x00000001
	NIF_ICON         = 0x00000002
	NIF_TIP          = 0x00000004
	NIF_INFO         = 0x00000010

	MF_STRING        = 0x00000000
	MF_SEPARATOR     = 0x00000800
	MF_CHECKED       = 0x00000008
	MF_UNCHECKED     = 0x00000000

	TPM_LEFTALIGN    = 0x0000
	TPM_BOTTOMALIGN  = 0x0020

	MONITOR_DEFAULTTONEAREST = 2

	SPI_SETDESKWALLPAPER = 0x0014
	SPIF_UPDATEINIFILE   = 0x01
	SPIF_SENDCHANGE      = 0x02

	HKEY_CURRENT_USER    = 0x80000001
	KEY_WRITE            = 0x00020006
	REG_SZ               = 1
)

const (
	ID_TRAY_OPEN_STUDIO      = 1000
	ID_TRAY_CHOOSE_WALLPAPER = 1001
	ID_TRAY_PLAY_PAUSE       = 1002
	ID_TRAY_MUTE             = 1003
	ID_TRAY_FULLSCREEN_PAUSE = 1004
	ID_TRAY_START_AT_LOGIN   = 1005
	ID_TRAY_ABOUT            = 1006
	ID_TRAY_STOP             = 1007
	ID_TRAY_EXIT             = 1008
)

type POINT struct {
	X int32
	Y int32
}

type RECT struct {
	Left   int32
	Top    int32
	Right  int32
	Bottom int32
}

type MONITORINFO struct {
	CbSize    uint32
	RcMonitor RECT
	RcWork    RECT
	DwFlags   uint32
}

type WNDCLASSEXW struct {
	CbSize        uint32
	Style         uint32
	LpfnWndProc   uintptr
	CbClsExtra    int32
	CbWndExtra    int32
	HInstance     syscall.Handle
	HIcon         syscall.Handle
	HCursor       syscall.Handle
	HbrBackground syscall.Handle
	LpszMenuName  *uint16
	LpszClassName *uint16
	HIconSm       syscall.Handle
}

type MSG struct {
	Hwnd    syscall.Handle
	Message uint32
	WParam  uintptr
	LParam  uintptr
	Time    uint32
	Pt      POINT
}

type NOTIFYICONDATAW struct {
	CbSize           uint32
	HWnd             syscall.Handle
	UID              uint32
	UFlags           uint32
	UCallbackMessage uint32
	HIcon            syscall.Handle
	SzTip            [128]uint16
	DwState          uint32
	DwStateMask      uint32
	SzInfo           [256]uint16
	TimeoutOrVersion uint32
	SzInfoTitle      [64]uint16
	DwInfoFlags      uint32
	GuidItem         [16]byte
	HBalloonIcon     syscall.Handle
}

type OPENFILENAMEW struct {
	LStructSize       uint32
	HwndOwner         syscall.Handle
	HInstance         syscall.Handle
	LpstrFilter       *uint16
	LpstrCustomFilter *uint16
	NMaxCustFilter    uint32
	NFilterIndex      uint32
	LpstrFile         *uint16
	NMaxFile          uint32
	LpstrFileTitle    *uint16
	NMaxFileTitle     uint32
	LpstrInitialDir   *uint16
	LpstrTitle        *uint16
	Flags             uint32
	NFileOffset       uint16
	NFileExtension    uint16
	LpstrDefExt       *uint16
	LCustData         uintptr
	LpfnHook          uintptr
	LpTemplateName    *uint16
	PvReserved        unsafe.Pointer
	DwReserved        uint32
	FlagsEx           uint32
}

type GalleryItem struct {
	ID       string `json:"id"`
	Path     string `json:"path"`
	Name     string `json:"name"`
	Format   string `json:"format"`
	Size     string `json:"size"`
	Duration string `json:"duration"`
	IsActive bool   `json:"is_active"`
}

type Config struct {
	ActiveVideoPath   string `json:"active_video_path"`
	IsPaused          bool   `json:"is_paused"`
	IsMuted           bool   `json:"is_muted"`
	PauseOnFullScreen bool   `json:"pause_on_fullscreen"`
	StartAtLogin      bool   `json:"start_at_login"`
	Volume            int    `json:"volume"`
}

var (
	appConfig         Config
	configPath        string
	galleryPath       string
	galleryItems      []GalleryItem
	serverPort        int
	mainWindowHwnd    syscall.Handle
	canvasHwnd        syscall.Handle
	workerWHwnd       syscall.Handle
	notifyIconData    NOTIFYICONDATAW
	isDesktopActive   bool
	isCurrentlyPaused bool
	isFsSuspended     bool
	resolvedWorkerW   syscall.Handle
)

func utf16Ptr(s string) *uint16 {
	p, _ := syscall.UTF16PtrFromString(s)
	return p
}

func mciSend(cmd string) error {
	pCmd, err := syscall.UTF16PtrFromString(cmd)
	if err != nil {
		return err
	}
	r, _, _ := procMciSendStringW.Call(uintptr(unsafe.Pointer(pCmd)), 0, 0, 0)
	if r != 0 {
		return fmt.Errorf("mci error: %d", r)
	}
	return nil
}

func getDesktopWorkerW() syscall.Handle {
	resolvedWorkerW = 0
	pProgman := utf16Ptr("Progman")
	progman, _, _ := procFindWindowW.Call(uintptr(unsafe.Pointer(pProgman)), 0)
	if progman == 0 {
		return 0
	}

	var result uintptr
	procSendMessageTimeoutW.Call(
		progman,
		0x052C,
		0xD,
		0x1,
		0,
		1000,
		uintptr(unsafe.Pointer(&result)),
	)

	enumProc := syscall.NewCallback(func(hwnd, lparam uintptr) uintptr {
		pShell := utf16Ptr("SHELLDLL_DefView")
		shellView, _, _ := procFindWindowExW.Call(hwnd, 0, uintptr(unsafe.Pointer(pShell)), 0)
		if shellView != 0 {
			pWorker := utf16Ptr("WorkerW")
			w, _, _ := procFindWindowExW.Call(0, hwnd, uintptr(unsafe.Pointer(pWorker)), 0)
			if w != 0 {
				resolvedWorkerW = syscall.Handle(w)
			}
		}
		return 1
	})

	procEnumWindows.Call(enumProc, 0)

	// Fallback for some Windows 11 builds
	if resolvedWorkerW == 0 {
		enumFallback := syscall.NewCallback(func(hwnd, lparam uintptr) uintptr {
			classNameBuf := make([]uint16, 256)
			procGetClassNameW.Call(hwnd, uintptr(unsafe.Pointer(&classNameBuf[0])), 256)
			cls := syscall.UTF16ToString(classNameBuf)
			if cls == "WorkerW" {
				pShell := utf16Ptr("SHELLDLL_DefView")
				shellView, _, _ := procFindWindowExW.Call(hwnd, 0, uintptr(unsafe.Pointer(pShell)), 0)
				if shellView == 0 {
					resolvedWorkerW = syscall.Handle(hwnd)
					return 0
				}
			}
			return 1
		})
		procEnumWindows.Call(enumFallback, 0)
	}

	return resolvedWorkerW
}

func refreshDesktop() {
	procSystemParametersInfoW.Call(SPI_SETDESKWALLPAPER, 0, 0, SPIF_UPDATEINIFILE|SPIF_SENDCHANGE)
}

func startWallpaper(videoPath string) bool {
	if _, err := os.Stat(videoPath); err != nil {
		return false
	}

	stopWallpaper()

	targetWorkerW := getDesktopWorkerW()
	if targetWorkerW == 0 {
		return false
	}
	workerWHwnd = targetWorkerW

	vx, _, _ := procGetSystemMetrics.Call(SM_XVIRTUALSCREEN)
	vy, _, _ := procGetSystemMetrics.Call(SM_YVIRTUALSCREEN)
	vw, _, _ := procGetSystemMetrics.Call(SM_CXVIRTUALSCREEN)
	vh, _, _ := procGetSystemMetrics.Call(SM_CYVIRTUALSCREEN)

	if vw == 0 || vh == 0 {
		vw, _, _ = procGetSystemMetrics.Call(SM_CXSCREEN)
		vh, _, _ = procGetSystemMetrics.Call(SM_CYSCREEN)
		vx = 0
		vy = 0
	}

	hInstance, _, _ := procGetModuleHandleW.Call(0)
	className := utf16Ptr("NORCanvasClass")

	canvas, _, _ := procCreateWindowExW.Call(
		0,
		uintptr(unsafe.Pointer(className)),
		uintptr(unsafe.Pointer(utf16Ptr("NOR Canvas"))),
		WS_CHILD|WS_VISIBLE|WS_CLIPSIBLINGS|WS_CLIPCHILDREN,
		vx, vy, vw, vh,
		uintptr(workerWHwnd),
		0,
		hInstance,
		0,
	)

	if canvas == 0 {
		return false
	}
	canvasHwnd = syscall.Handle(canvas)

	// Set extended styles: Click-through, transparent to mouse events, unactivatable
	procSetWindowLongPtrW.Call(
		uintptr(canvasHwnd),
		uintptr(0xFFFFFFFFFFFFFFEC), // GWL_EXSTYLE = -20
		WS_EX_TOOLWINDOW|WS_EX_TRANSPARENT|WS_EX_NOACTIVATE,
	)

	procSetWindowPos.Call(
		uintptr(canvasHwnd),
		1, // HWND_BOTTOM
		vx, vy, vw, vh,
		SWP_SHOWWINDOW|SWP_NOACTIVATE|SWP_FRAMECHANGED,
	)

	// Open video through MCI hardware acceleration
	mciSend("close nor_video")
	openCmd := fmt.Sprintf("open \"%s\" type mpegvideo alias nor_video style child parent %d", videoPath, canvasHwnd)
	if err := mciSend(openCmd); err != nil {
		mciSend(fmt.Sprintf("open \"%s\" alias nor_video style child parent %d", videoPath, canvasHwnd))
	}

	mciSend(fmt.Sprintf("put nor_video window at 0 0 %d %d", vw, vh))
	mciSend("play nor_video repeat")

	if appConfig.IsMuted {
		mciSend("setaudio nor_video off")
	} else {
		mciSend("setaudio nor_video on")
		mciSend(fmt.Sprintf("setaudio nor_video volume to %d", appConfig.Volume))
	}

	isDesktopActive = true
	isCurrentlyPaused = false
	appConfig.ActiveVideoPath = videoPath
	saveConfig()
	updateGalleryActiveState(videoPath)
	return true
}

func stopWallpaper() {
	if isDesktopActive {
		mciSend("stop nor_video")
		mciSend("close nor_video")
		if canvasHwnd != 0 {
			procDestroyWindow.Call(uintptr(canvasHwnd))
			canvasHwnd = 0
		}
		isDesktopActive = false
		appConfig.ActiveVideoPath = ""
		saveConfig()
		updateGalleryActiveState("")
		refreshDesktop()
	}
}

func togglePlayPause() {
	if !isDesktopActive {
		if appConfig.ActiveVideoPath != "" {
			startWallpaper(appConfig.ActiveVideoPath)
		}
		return
	}

	if isCurrentlyPaused {
		mciSend("resume nor_video")
		isCurrentlyPaused = false
	} else {
		mciSend("pause nor_video")
		isCurrentlyPaused = true
	}
}

func toggleMute() {
	appConfig.IsMuted = !appConfig.IsMuted
	if appConfig.IsMuted {
		mciSend("setaudio nor_video off")
	} else {
		mciSend("setaudio nor_video on")
		mciSend(fmt.Sprintf("setaudio nor_video volume to %d", appConfig.Volume))
	}
	saveConfig()
}

func chooseWallpaperDialog(owner syscall.Handle) string {
	buf := make([]uint16, 1024)
	filter := "Video Files (*.mp4;*.mov;*.wmv;*.avi;*.mkv)\x00*.mp4;*.mov;*.wmv;*.avi;*.mkv\x00All Files (*.*)\x00*.*\x00\x00"

	ofn := OPENFILENAMEW{
		HwndOwner:   owner,
		LpstrFilter: utf16Ptr(filter),
		LpstrFile:   &buf[0],
		NMaxFile:    1024,
		Flags:       0x00000800 | 0x00000004 | 0x00001000, // OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST | OFN_EXPLORER
	}
	ofn.LStructSize = uint32(unsafe.Sizeof(ofn))

	r, _, _ := procGetOpenFileNameW.Call(uintptr(unsafe.Pointer(&ofn)))
	if r != 0 {
		return syscall.UTF16ToString(buf)
	}
	return ""
}

func isForegroundFullscreen() bool {
	fg, _, _ := procGetForegroundWindow.Call()
	if fg == 0 {
		return false
	}

	isMin, _, _ := procIsIconic.Call(fg)
	if isMin != 0 {
		return false
	}

	classNameBuf := make([]uint16, 256)
	procGetClassNameW.Call(fg, uintptr(unsafe.Pointer(&classNameBuf[0])), 256)
	cls := syscall.UTF16ToString(classNameBuf)
	if cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" {
		return false
	}

	var appRect RECT
	procGetWindowRect.Call(fg, uintptr(unsafe.Pointer(&appRect)))

	hMon, _, _ := procMonitorFromWindow.Call(fg, MONITOR_DEFAULTTONEAREST)
	var mi MONITORINFO
	mi.CbSize = uint32(unsafe.Sizeof(mi))
	procGetMonitorInfoW.Call(hMon, uintptr(unsafe.Pointer(&mi)))

	return (appRect.Left <= mi.RcMonitor.Left &&
		appRect.Top <= mi.RcMonitor.Top &&
		appRect.Right >= mi.RcMonitor.Right &&
		appRect.Bottom >= mi.RcMonitor.Bottom)
}

func startFullscreenMonitor() {
	go func() {
		for {
			time.Sleep(500 * time.Millisecond)
			if !appConfig.PauseOnFullScreen || !isDesktopActive {
				continue
			}

			fs := isForegroundFullscreen()
			if fs && !isFsSuspended {
				isFsSuspended = true
				mciSend("pause nor_video")
			} else if !fs && isFsSuspended {
				isFsSuspended = false
				if !isCurrentlyPaused {
					mciSend("resume nor_video")
				}
			}
		}
	}()
}

func showContextMenu(hwnd syscall.Handle) {
	var pt POINT
	procGetCursorPos.Call(uintptr(unsafe.Pointer(&pt)))

	hMenu, _, _ := procCreatePopupMenu.Call()

	procAppendMenuW.Call(hMenu, MF_STRING, ID_TRAY_OPEN_STUDIO, uintptr(unsafe.Pointer(utf16Ptr("🖥 Open NŌR Studio"))))
	procAppendMenuW.Call(hMenu, MF_SEPARATOR, 0, 0)

	procAppendMenuW.Call(hMenu, MF_STRING, ID_TRAY_CHOOSE_WALLPAPER, uintptr(unsafe.Pointer(utf16Ptr("🌟 Choose Video Wallpaper..."))))
	procAppendMenuW.Call(hMenu, MF_SEPARATOR, 0, 0)

	pauseText := "⏸ Pause Live Wallpaper"
	if isCurrentlyPaused {
		pauseText = "▶ Resume Live Wallpaper"
	}
	procAppendMenuW.Call(hMenu, MF_STRING, ID_TRAY_PLAY_PAUSE, uintptr(unsafe.Pointer(utf16Ptr(pauseText))))

	muteText := "🔇 Mute Wallpaper Sound"
	if appConfig.IsMuted {
		muteText = "🔊 Unmute Wallpaper Sound"
	}
	procAppendMenuW.Call(hMenu, MF_STRING, ID_TRAY_MUTE, uintptr(unsafe.Pointer(utf16Ptr(muteText))))

	procAppendMenuW.Call(hMenu, MF_SEPARATOR, 0, 0)

	fsFlag := uintptr(MF_UNCHECKED)
	if appConfig.PauseOnFullScreen {
		fsFlag = MF_CHECKED
	}
	procAppendMenuW.Call(hMenu, MF_STRING|fsFlag, ID_TRAY_FULLSCREEN_PAUSE, uintptr(unsafe.Pointer(utf16Ptr("⚡ Zero-Drain Fullscreen Pause"))))

	bootFlag := uintptr(MF_UNCHECKED)
	if appConfig.StartAtLogin {
		bootFlag = MF_CHECKED
	}
	procAppendMenuW.Call(hMenu, MF_STRING|bootFlag, ID_TRAY_START_AT_LOGIN, uintptr(unsafe.Pointer(utf16Ptr("🚀 Launch at Windows Startup"))))

	procAppendMenuW.Call(hMenu, MF_SEPARATOR, 0, 0)
	procAppendMenuW.Call(hMenu, MF_STRING, ID_TRAY_STOP, uintptr(unsafe.Pointer(utf16Ptr("⏹ Stop Wallpaper"))))
	procAppendMenuW.Call(hMenu, MF_STRING, ID_TRAY_ABOUT, uintptr(unsafe.Pointer(utf16Ptr("ℹ About NŌR..."))))
	procAppendMenuW.Call(hMenu, MF_SEPARATOR, 0, 0)
	procAppendMenuW.Call(hMenu, MF_STRING, ID_TRAY_EXIT, uintptr(unsafe.Pointer(utf16Ptr("❌ Exit NŌR"))))

	procSetForegroundWindow.Call(uintptr(hwnd))
	procTrackPopupMenuEx.Call(hMenu, TPM_LEFTALIGN|TPM_BOTTOMALIGN, uintptr(pt.X), uintptr(pt.Y), uintptr(hwnd), 0)
	procDestroyMenu.Call(hMenu)
}

func mainWndProc(hwnd syscall.Handle, msg uint32, wParam, lParam uintptr) uintptr {
	switch msg {
	case WM_TRAYICON:
		switch lParam {
		case WM_RBUTTONUP:
			showContextMenu(hwnd)
		case WM_LBUTTONUP, WM_LBUTTONDBLCLK:
			launchStudioWindow(serverPort)
		}
		return 0

	case WM_COMMAND:
		switch wParam {
		case ID_TRAY_OPEN_STUDIO:
			launchStudioWindow(serverPort)
		case ID_TRAY_CHOOSE_WALLPAPER:
			path := chooseWallpaperDialog(hwnd)
			if path != "" {
				addFileToGallery(path)
				startWallpaper(path)
			}
		case ID_TRAY_PLAY_PAUSE:
			togglePlayPause()
		case ID_TRAY_MUTE:
			toggleMute()
		case ID_TRAY_FULLSCREEN_PAUSE:
			appConfig.PauseOnFullScreen = !appConfig.PauseOnFullScreen
			saveConfig()
		case ID_TRAY_START_AT_LOGIN:
			appConfig.StartAtLogin = !appConfig.StartAtLogin
			setStartupRegistry(appConfig.StartAtLogin)
			saveConfig()
		case ID_TRAY_STOP:
			stopWallpaper()
		case ID_TRAY_ABOUT:
			aboutText := "NŌR - Modern Dynamic Live Wallpaper Engine for Windows\n\nHardware-accelerated zero-copy video playback directly behind Windows desktop icons with zero idle GPU consumption.\n\nCrafted with precision."
			procMessageBoxW.Call(uintptr(hwnd), uintptr(unsafe.Pointer(utf16Ptr(aboutText))), uintptr(unsafe.Pointer(utf16Ptr("About NŌR"))), 0x40)
		case ID_TRAY_EXIT:
			stopWallpaper()
			procShell_NotifyIconW.Call(NIM_DELETE, uintptr(unsafe.Pointer(&notifyIconData)))
			procPostQuitMessage.Call(0)
		}
		return 0

	case WM_DESTROY:
		stopWallpaper()
		procShell_NotifyIconW.Call(NIM_DELETE, uintptr(unsafe.Pointer(&notifyIconData)))
		procPostQuitMessage.Call(0)
		return 0
	}

	r, _, _ := procDefWindowProcW.Call(uintptr(hwnd), uintptr(msg), wParam, lParam)
	return r
}

func canvasWndProc(hwnd syscall.Handle, msg uint32, wParam, lParam uintptr) uintptr {
	r, _, _ := procDefWindowProcW.Call(uintptr(hwnd), uintptr(msg), wParam, lParam)
	return r
}

func setStartupRegistry(enable bool) {
	exePath, err := os.Executable()
	if err != nil {
		return
	}

	pSubKey := utf16Ptr(`Software\Microsoft\Windows\CurrentVersion\Run`)
	var hKey uintptr
	r, _, _ := procRegOpenKeyExW.Call(HKEY_CURRENT_USER, uintptr(unsafe.Pointer(pSubKey)), 0, KEY_WRITE, uintptr(unsafe.Pointer(&hKey)))
	if r != 0 {
		return
	}
	defer procRegCloseKey.Call(hKey)

	pValName := utf16Ptr("NORLiveWallpaper")
	if enable {
		val := fmt.Sprintf("\"%s\"", exePath)
		pValData := utf16Ptr(val)
		byteLen := uint32((len(val) + 1) * 2)
		procRegSetValueExW.Call(hKey, uintptr(unsafe.Pointer(pValName)), 0, REG_SZ, uintptr(unsafe.Pointer(pValData)), uintptr(byteLen))
	} else {
		procRegDeleteValueW.Call(hKey, uintptr(unsafe.Pointer(pValName)))
	}
}

func loadConfig() {
	appData := os.Getenv("APPDATA")
	if appData == "" {
		appData = "."
	}
	norDir := filepath.Join(appData, "NOR")
	os.MkdirAll(norDir, 0755)
	configPath = filepath.Join(norDir, "config.json")
	galleryPath = filepath.Join(norDir, "gallery.json")

	appConfig = Config{
		PauseOnFullScreen: true,
		Volume:            800,
	}

	if data, err := os.ReadFile(configPath); err == nil {
		json.Unmarshal(data, &appConfig)
	}

	if data, err := os.ReadFile(galleryPath); err == nil {
		json.Unmarshal(data, &galleryItems)
	}
}

func saveConfig() {
	if configPath != "" {
		if data, err := json.MarshalIndent(appConfig, "", "  "); err == nil {
			os.WriteFile(configPath, data, 0644)
		}
	}
	if galleryPath != "" {
		if data, err := json.MarshalIndent(galleryItems, "", "  "); err == nil {
			os.WriteFile(galleryPath, data, 0644)
		}
	}
}

func updateGalleryActiveState(activePath string) {
	for i := range galleryItems {
		galleryItems[i].IsActive = (galleryItems[i].Path == activePath)
	}
	saveConfig()
}

func addFileToGallery(path string) GalleryItem {
	fi, err := os.Stat(path)
	sizeStr := "10 MB"
	if err == nil {
		mb := float64(fi.Size()) / (1024.0 * 1024.0)
		if mb >= 1000 {
			sizeStr = fmt.Sprintf("%.2f GB", mb/1024.0)
		} else {
			sizeStr = fmt.Sprintf("%.1f MB", mb)
		}
	}

	ext := strings.ToUpper(strings.TrimPrefix(filepath.Ext(path), "."))
	name := strings.TrimSuffix(filepath.Base(path), filepath.Ext(path))
	id := fmt.Sprintf("%d", time.Now().UnixNano())

	item := GalleryItem{
		ID:       id,
		Path:     path,
		Name:     name,
		Format:   ext,
		Size:     sizeStr,
		Duration: "0:15",
		IsActive: false,
	}

	// Avoid duplicates
	for _, existing := range galleryItems {
		if existing.Path == path {
			return existing
		}
	}

	galleryItems = append([]GalleryItem{item}, galleryItems...)
	saveConfig()
	return item
}

// GUI Launcher (Native Windows Edge/Chrome App Mode)
func getBrowserAppPath() (string, bool) {
	edgePaths := []string{
		`C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe`,
		`C:\Program Files\Microsoft\Edge\Application\msedge.exe`,
		os.Getenv("LOCALAPPDATA") + `\Microsoft\Edge\Application\msedge.exe`,
	}
	for _, p := range edgePaths {
		if _, err := os.Stat(p); err == nil {
			return p, true
		}
	}

	chromePaths := []string{
		`C:\Program Files\Google\Chrome\Application\chrome.exe`,
		`C:\Program Files (x86)\Google\Chrome\Application\chrome.exe`,
		os.Getenv("LOCALAPPDATA") + `\Google\Chrome\Application\chrome.exe`,
	}
	for _, p := range chromePaths {
		if _, err := os.Stat(p); err == nil {
			return p, true
		}
	}

	return "msedge", false
}

func launchStudioWindow(port int) {
	url := fmt.Sprintf("http://127.0.0.1:%d", port)
	browser, isDirect := getBrowserAppPath()

	appData := os.Getenv("LOCALAPPDATA")
	if appData == "" {
		appData = "."
	}
	profileDir := filepath.Join(appData, "NOR", "Profile")
	os.MkdirAll(profileDir, 0755)

	appArg := fmt.Sprintf("--app=%s", url)
	sizeArg := "--window-size=1080,720"
	userDirArg := fmt.Sprintf("--user-data-dir=%s", profileDir)

	if isDirect {
		cmd := exec.Command(browser, appArg, sizeArg, userDirArg)
		cmd.Start()
	} else {
		cmd := exec.Command("cmd", "/c", "start", browser, appArg, sizeArg, userDirArg)
		if err := cmd.Start(); err != nil {
			exec.Command("rundll32", "url.dll,FileProtocolHandler", url).Start()
		}
	}
}

// Embedded Local HTTP Server for GUI
func startHttpServer() int {
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		return 8080
	}
	port := listener.Addr().(*net.TCPAddr).Port

	mux := http.NewServeMux()

	// Serve Studio GUI
	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		data, err := uiFS.ReadFile("ui/index.html")
		if err != nil {
			http.Error(w, "Not found", 404)
			return
		}
		w.Header().Set("Content-Type", "text/html; charset=utf-8")
		w.Write(data)
	})

	// Serve Logo
	mux.HandleFunc("/api/logo", func(w http.ResponseWriter, r *http.Request) {
		data, err := uiFS.ReadFile("ui/logo.png")
		if err != nil {
			http.Error(w, "Not found", 404)
			return
		}
		w.Header().Set("Content-Type", "image/png")
		w.Write(data)
	})

	// Stream Local Video File with HTTP Range support
	mux.HandleFunc("/media", func(w http.ResponseWriter, r *http.Request) {
		filePath := r.URL.Query().Get("path")
		if filePath == "" || !filepath.IsAbs(filePath) {
			http.Error(w, "Invalid path", 400)
			return
		}
		http.ServeFile(w, r, filePath)
	})

	// API: Status
	mux.HandleFunc("/api/status", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"is_active":            isDesktopActive,
			"is_paused":            isCurrentlyPaused,
			"is_muted":             appConfig.IsMuted,
			"volume":               appConfig.Volume,
			"pause_on_fullscreen":  appConfig.PauseOnFullScreen,
			"start_at_login":       appConfig.StartAtLogin,
			"active_path":          appConfig.ActiveVideoPath,
		})
	})

	// API: Gallery
	mux.HandleFunc("/api/gallery", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(galleryItems)
	})

	// API: Import Media (calls native Windows file dialog)
	mux.HandleFunc("/api/import", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		filePath := chooseWallpaperDialog(mainWindowHwnd)
		if filePath != "" {
			item := addFileToGallery(filePath)
			json.NewEncoder(w).Encode(item)
			return
		}
		w.Write([]byte("{}"))
	})

	// API: Delete from Gallery
	mux.HandleFunc("/api/delete", func(w http.ResponseWriter, r *http.Request) {
		id := r.URL.Query().Get("id")
		newItems := make([]GalleryItem, 0, len(galleryItems))
		for _, item := range galleryItems {
			if item.ID == id {
				if item.Path == appConfig.ActiveVideoPath {
					stopWallpaper()
				}
				continue
			}
			newItems = append(newItems, item)
		}
		galleryItems = newItems
		saveConfig()
		w.Write([]byte(`{"success": true}`))
	})

	// API: Set as Desktop Wallpaper
	mux.HandleFunc("/api/set-desktop", func(w http.ResponseWriter, r *http.Request) {
		id := r.URL.Query().Get("id")
		for _, item := range galleryItems {
			if item.ID == id {
				startWallpaper(item.Path)
				w.Write([]byte(`{"success": true}`))
				return
			}
		}
		http.Error(w, "Not found", 404)
	})

	// API: Pause / Resume / Stop
	mux.HandleFunc("/api/pause", func(w http.ResponseWriter, r *http.Request) {
		if isDesktopActive && !isCurrentlyPaused {
			togglePlayPause()
		}
		w.Write([]byte(`{"success": true}`))
	})

	mux.HandleFunc("/api/resume", func(w http.ResponseWriter, r *http.Request) {
		if isDesktopActive && isCurrentlyPaused {
			togglePlayPause()
		}
		w.Write([]byte(`{"success": true}`))
	})

	mux.HandleFunc("/api/stop", func(w http.ResponseWriter, r *http.Request) {
		stopWallpaper()
		w.Write([]byte(`{"success": true}`))
	})

	// API: Volume
	mux.HandleFunc("/api/volume", func(w http.ResponseWriter, r *http.Request) {
		valStr := r.URL.Query().Get("val")
		if v, err := strconv.Atoi(valStr); err == nil {
			appConfig.Volume = v
			if isDesktopActive && !appConfig.IsMuted {
				mciSend(fmt.Sprintf("setaudio nor_video volume to %d", appConfig.Volume))
			}
			saveConfig()
		}
		w.Write([]byte(`{"success": true}`))
	})

	// API: Settings
	mux.HandleFunc("/api/settings", func(w http.ResponseWriter, r *http.Request) {
		fsStr := r.URL.Query().Get("fullscreen")
		bootStr := r.URL.Query().Get("startup")

		appConfig.PauseOnFullScreen = (fsStr == "true")
		appConfig.StartAtLogin = (bootStr == "true")
		setStartupRegistry(appConfig.StartAtLogin)
		saveConfig()
		w.Write([]byte(`{"success": true}`))
	})

	go http.Serve(listener, mux)
	return port
}

func main() {
	loadConfig()

	// Start embedded HTTP GUI Server
	serverPort = startHttpServer()

	hInstance, _, _ := procGetModuleHandleW.Call(0)

	// Register Main Message-only Window Class
	mainClassName := utf16Ptr("NORMainClass")
	mainWndClass := WNDCLASSEXW{
		CbSize:        uint32(unsafe.Sizeof(WNDCLASSEXW{})),
		LpfnWndProc:   syscall.NewCallback(mainWndProc),
		HInstance:     syscall.Handle(hInstance),
		LpszClassName: mainClassName,
	}
	procRegisterClassExW.Call(uintptr(unsafe.Pointer(&mainWndClass)))

	// Register Canvas Window Class
	canvasClassName := utf16Ptr("NORCanvasClass")
	canvasWndClass := WNDCLASSEXW{
		CbSize:        uint32(unsafe.Sizeof(WNDCLASSEXW{})),
		LpfnWndProc:   syscall.NewCallback(canvasWndProc),
		HInstance:     syscall.Handle(hInstance),
		LpszClassName: canvasClassName,
	}
	procRegisterClassExW.Call(uintptr(unsafe.Pointer(&canvasWndClass)))

	// Create Main Message Window
	hwnd, _, _ := procCreateWindowExW.Call(
		0,
		uintptr(unsafe.Pointer(mainClassName)),
		uintptr(unsafe.Pointer(utf16Ptr("NŌR Background Service"))),
		0,
		0, 0, 0, 0,
		0, 0, hInstance, 0,
	)
	mainWindowHwnd = syscall.Handle(hwnd)

	// Load System Icon
	var hIconSmall uintptr
	procExtractIconExW.Call(uintptr(unsafe.Pointer(utf16Ptr("shell32.dll"))), 131, 0, uintptr(unsafe.Pointer(&hIconSmall)), 1)

	// Create System Tray Icon
	notifyIconData = NOTIFYICONDATAW{
		CbSize:           uint32(unsafe.Sizeof(NOTIFYICONDATAW{})),
		HWnd:             mainWindowHwnd,
		UID:              1,
		UFlags:           NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_INFO,
		UCallbackMessage: WM_TRAYICON,
		HIcon:            syscall.Handle(hIconSmall),
	}
	tipStr, _ := syscall.UTF16FromString("NŌR - Dynamic Live Wallpaper Engine")
	copy(notifyIconData.SzTip[:], tipStr)

	balloonTitle, _ := syscall.UTF16FromString("NŌR Live Wallpaper")
	copy(notifyIconData.SzInfoTitle[:], balloonTitle)

	balloonInfo, _ := syscall.UTF16FromString("NŌR Studio is running. Click the tray icon to open the Studio.")
	copy(notifyIconData.SzInfo[:], balloonInfo)

	notifyIconData.DwInfoFlags = 0x00000001
	procShell_NotifyIconW.Call(NIM_ADD, uintptr(unsafe.Pointer(&notifyIconData)))

	// Start Fullscreen Watcher
	startFullscreenMonitor()

	// Autostart last active wallpaper if valid
	if appConfig.ActiveVideoPath != "" {
		if _, err := os.Stat(appConfig.ActiveVideoPath); err == nil {
			startWallpaper(appConfig.ActiveVideoPath)
		}
	}

	// Launch NŌR Studio GUI Window on startup!
	go launchStudioWindow(serverPort)

	// Message Loop
	var msg MSG
	for {
		r, _, _ := procGetMessageW.Call(uintptr(unsafe.Pointer(&msg)), 0, 0, 0)
		if int32(r) <= 0 {
			break
		}
		procTranslateMessage.Call(uintptr(unsafe.Pointer(&msg)))
		procDispatchMessageW.Call(uintptr(unsafe.Pointer(&msg)))
	}
}
