# NutriLens 🥗

**A cross-platform mobile nutrition tracking app built with .NET MAUI**

NutriLens helps you track your daily food intake, scan food items for nutritional information, and make healthier eating choices. The app combines AI-powered food recognition, barcode scanning, and smart health scoring to give you a complete picture of your daily nutrition.

---

## Author

Name: **Yu Fang**

Student ID: **21906400**

Module: 6G6Z0014 – Mobile Computing  

---

## App Overview

NutriLens is a Food & Drink themed mobile application that allows users to:

- Scan food barcodes or take photos to retrieve nutritional information
- Log daily meals (Breakfast, Lunch, Dinner, Snacks)
- Track calorie and water intake with visual progress indicators
- Receive a daily health score based on nutrition and hydration
- Browse and manage a personal food database with category navigation
- Find nearby healthy restaurants using GPS
- Get random meal suggestions by shaking the device
- Customise allergen warnings, nutrition goals and accessibility settings

---

## Features

### 🏠 Home Page
- Daily health score (0–10) based on calorie and water intake
- Calorie and water intake progress bars
- Quick +250ml water logging button with goal notification
- Today's Breakfast / Lunch / Dinner summary cards
- Shake to get a random healthy meal suggestion

### 🍎 Food Database Page
- Left side category navigation (All, Meat, Fish, Vegetables, Fruits, Dairy, Grains, Snacks, Drinks, Other)
- Search bar to filter foods by name or category
- Statistics: Total foods, Average calories, Number of categories
- Tap a food card to view full details
- Swipe right to edit, swipe left to delete
- Add custom foods with full nutrition details
- Pull to refresh

### 📷 Scanner Page
- **AI Photo Recognition** — Take a photo of food and identify it using the Qwen Vision API
- **Barcode Scanner** — Real-time camera barcode scanning using ZXing.Net.Maui
- **Flash** — Toggle the device flashlight on/off during barcode scanning
- **Manual Barcode Entry** — Type a barcode number and search Open Food Facts database
- Nutrition display: Calories, Protein, Fat, Sugar
- Allergen detection with vibration warning
- Text-to-Speech reads nutrition information aloud
- Save scanned food to Breakfast, Lunch or Dinner

### 📅 Diary Page
- View all food entries logged today
- Daily totals: Calories, Protein, Fat
- Manual food entry (name, calories, meal type)
- Swipe left to delete entries with confirmation
- Pull down to refresh

### 📍 Nearby Page
- GPS geolocation to detect current position
- Reverse geocoding to display readable address
- Search for nearby healthy restaurants with customisable keyword and radius
- Results show name, address, distance and rating
- Tap a result card for full details
- Pull to refresh

### ⚙️ Settings Page
- **Theme**: Light Mode / Dark Mode / Follow System (Radio buttons)
- **Font Size**: Adjustable slider (12–24pt) — applied globally across all pages
- **Text-to-Speech**: Toggle on/off
- **Custom Allergens**: Add/remove allergens with swipe-to-delete
- **Nutrition Goals**: Set daily calorie and water targets with validation
- **Water Reminder**: Hourly vibration reminder when daily water target is not met
- WCAG accessibility guidelines referenced throughout

---

## Hardware Features Used

| Hardware | Where Used | Description |
|----------|-----------|-------------|
| **Camera** | Scanner Page | Take photos for AI food recognition and barcode scanning |
| **Flash** | Barcode Scanner Page | Toggle device flashlight on/off for better scanning in low light |
| **Accelerometer / Shake** | Home Page | Shake the phone to receive a random meal suggestion |
| **Geolocation** | Nearby Page | Get current location coordinates and readable address |
| **Text-to-Speech** | Multiple Pages | Read nutrition information aloud for accessibility |
| **Vibration / Haptic Feedback** | Multiple Pages | Confirm actions, allergen warnings, water reminders, error feedback |

**Total: 6 hardware features** 

---

## Accessibility Features

This app follows the **Web Content Accessibility Guidelines (WCAG 2.1)**:

| Guideline | Implementation |
|-----------|----------------|
| **WCAG 1.4.3** — Contrast Ratio | High contrast text on all card backgrounds; dark mode support |
| **WCAG 1.4.4** — Resize Text | Global font size slider (12–24pt) applies to all pages dynamically |
| **WCAG 1.1.1** — Non-text Content | Text-to-Speech reads nutritional information aloud |
| **WCAG 1.4.1** — Use of Colour | Status conveyed by text labels in addition to colour |
| **WCAG 2.4.3** — Focus Order | Logical tab/focus order throughout all pages |

Additional accessibility features:
- Dark mode support across all pages
- Haptic vibration feedback for key interactions
- Semantic hints on interactive elements (SemanticProperties.Hint)
- Allergen warnings are highlighted in red

---

## Technical Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET MAUI (.NET 8) |
| UI | XAML |
| Local Database | SQLite (sqlite-net-pcl) |
| Barcode Scanning | ZXing.Net.Maui.Controls 0.4.0 |
| UI Toolkit | CommunityToolkit.Maui 9.1.1 |
| Food AI Recognition | Qwen Vision API (qwen-vl-plus) |
| Nutrition Data | Open Food Facts API |
| Architecture | Service layer + Code-behind (MVVM-inspired) |

---

## How to Run

### Requirements
- Visual Studio 2022 (17.8+)
- .NET MAUI workload installed
- Android SDK (API 21+)
- Windows 10.0.17763+

### Steps

1. Clone the repository:
```
git clone https://github.com/HubuManchester/fooddrink-Freya-fy.git
cd NutriLens
```

2. Open `NutriLens.sln` in Visual Studio 2022

3. Restore NuGet packages (automatic on build)

4. Select target platform:
   - **Android**: Connect a physical device or start an emulator
   - **Windows**: Select "Windows Machine"

5. Press **F5** or click the Run button

### Notes
- GPS and Camera features require a physical Android device or an emulator with location spoofing enabled
- The Qwen AI food recognition requires an active internet connection

---

## Deployment

The app has been tested and deployed on:

| Platform | Device | Status |
|----------|--------|--------|
| Android | Physical device (Huawei) | ✅ Full functionality |
| Windows | Windows 11 PC | ✅ Core functionality (GPS not available on Windows) |

---

## Development Plan

### Completed Features
- [x] Project setup with .NET MAUI and all NuGet packages
- [x] Bottom navigation with 5 tabs
- [x] Home page with health score and water tracking
- [x] SQLite local database for diary entries and settings
- [x] Scanner page with Qwen AI photo recognition
- [x] Real barcode scanner using ZXing with torch support
- [x] Open Food Facts API integration
- [x] Allergen warning system with vibration
- [x] Text-to-Speech nutrition reading
- [x] Diary page with manual entry and swipe-to-delete
- [x] Food Database page with category navigation and CRUD
- [x] Nearby page with GPS and reverse geocoding
- [x] Settings page with dark mode, font size, allergens and goals
- [x] Full dark mode support across all pages
- [x] Dynamic font size binding (WCAG 1.4.4)
- [x] Android and Windows deployment

### Known Limitations
- Barcode search is limited to products in the Open Food Facts database (primarily Western products)
- GPS and Camera hardware features are not available on Windows platform
- AI food recognition accuracy depends on image quality and lighting

---

## Project Structure

```
NutriLens/
├── Models/
│   ├── DiaryEntry.cs          # SQLite model for food diary entries
│   ├── FoodItem.cs            # SQLite model for food database items
│   └── UserSettings.cs        # SQLite model for user preferences
├── Services/
│   └── DatabaseService.cs     # SQLite CRUD operations
├── Views/
│   ├── HomePage.xaml          # Daily summary and health score
│   ├── ScannerPage.xaml       # Photo and barcode food scanning
│   ├── BarcodeScannerPage.xaml # Real-time ZXing camera scanner
│   ├── DiaryPage.xaml         # Food diary with daily totals
│   ├── FoodDatabasePage.xaml  # Browsable food database with CRUD
│   ├── NearbyPage.xaml        # GPS-based nearby restaurant finder
│   └── SettingsPage.xaml      # App preferences and accessibility
├── App.xaml                   # Global resources and theme
├── AppShell.xaml              # Navigation shell and tab bar
└── MauiProgram.cs             # App builder and dependency injection
```

