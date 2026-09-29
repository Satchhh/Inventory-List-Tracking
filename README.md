# Inventory Entry App — rough sketch

A starting point for the WPF app: a form to enter inventory items and
save them straight to an Excel file, with a duplicate-serial warning
and quick "copy" buttons.

## Before you can open/run this

- **.NET 10 SDK**, plus Visual Studio 2022/2026 with the ".NET desktop
  development" workload (for WPF). If the machine you're using only
  has an older SDK, either install .NET 10 or change the
  `<TargetFramework>` line in `InventoryApp.csproj` back to
  `net8.0-windows` — the rest of the code doesn't use anything
  .NET-10-specific.
- Open the folder in Visual Studio (File → Open → Folder, or open the
  `.csproj` directly) and it will restore the ClosedXML NuGet package
  automatically on first build.
- WPF only builds on Windows, so this can't be compiled or previewed
  outside Visual Studio on a Windows machine.

## What's here

```
InventoryApp/
  InventoryApp.csproj
  App.xaml / App.xaml.cs        shared colors, fonts, button styles
  MainWindow.xaml(.cs)          the entry form
  Models/InventoryItem.cs       one inventory row + the normalize/EAN helpers
  Services/ExcelStore.cs        all reading/writing of the .xlsx file
  Views/DuplicateWarningWindow  the "serial already exists" popup
```

Everything that touches the Excel file goes through `ExcelStore`. If
you switch to SQLite or a shared database later, that's the only
class that needs rewriting — `MainWindow` doesn't know or care how
the data is actually stored.

## What it does right now

- Serial Number, Article Number, Type/Model, Category, Description,
  Country of Manufacture, Supplier, Location, Remarks
- "No serial number on this item" checkbox — ticking it disables and
  clears the Serial box, and that row is skipped by the duplicate check
- Duplicate check is on **Serial Number only** (not Article Number,
  since many items legitimately share one article number). It's a
  **warning with Save Anyway**, not a hard block, because the real
  inventory sheet had a few legitimate repeats (shared lot/date codes
  entered as serials on some older rows)
- Article numbers are compared after removing spaces, hyphens and a
  leading "1P", so "1P 6ES7 407-0DA01-0AA0" and "6ES74070DA010AA0"
  count as the same article — this only affects the comparison, the
  original text you typed is still what gets saved
- Serial and Article Number are saved as **text**, so Excel won't eat
  a leading zero or turn a long number into scientific notation
- A `.bak` copy of the Excel file is made right before each save
- Location is *not* cleared after Save, since consecutive items are
  often from the same cabinet — everything else clears for the next entry
- Copy buttons next to Serial and Article, plus a "Copy Row" button
  that puts every field on the clipboard as one tab-separated line —
  paste it into the first cell of an Excel row and it fills across

## Assumptions I made — check these with your team

1. **"Copy button" meaning.** I built it as: copy a value *out* of the
   app with one click, instead of dragging the mouse to select it. If
   you actually meant pasting data *into* the app faster, that's a
   different feature (e.g. a "paste from clipboard, auto-split into
   fields" button) — tell me and I'll build that instead.
2. **Where the Excel file lives.** Right now it just writes
   `Inventory.xlsx` next to the app. Change the path in
   `MainWindow.cs` (`_store = new ExcelStore(...)`) once you know
   where the real shared file should be.
3. **Category list.** The dropdown values came from cleaning up the
   ~70 messy "Type" values in your real sheet down to about 16
   categories. Edit the `<ComboBoxItem>` list in `MainWindow.xaml` to
   match whatever your team agrees on.
4. **Country/Location lists** are just a few placeholder examples —
   add your actual list.

## Not built yet (known gaps, on purpose — this is a rough sketch)

- No barcode scanner handling or EAN auto-detection in the UI yet
  (the `Normalize.IsValidEan13` helper in `InventoryItem.cs` is ready
  to use for this later)
- No edit/delete of existing rows
- No login or "entered by" tracking
- No format-warning for article numbers that don't look like a usual
  Siemens/other-brand pattern
- Error handling is minimal — good enough to test with, not
  production-hardened yet
