
# About:
This is a fork of DC Font Generator (https://sourceforge.net/projects/dcfontgenerator/).

# Requirement
[.NET Desktop Runtime 8.0.18](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

# Changelog:
- Recompile with .NET 8.0.
- Fix control characters mapping (finally).
- Optimize some code.
- Improve font drawing quality with ClearTypeGridFit.
- Select font size in px. And now show font's size and height after selected.
- Use font's original height for line spacing.

# Known Issues:
- Character's horizontal spacing look bad when genreate font with glow or outline effect.(Original DCFG issue).
