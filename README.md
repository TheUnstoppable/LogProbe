# LogProbe
LogProbe is a tool to read, test and diagnose SSGM/Dragonade Log Server.

## Get started
LogProbe uses command line arguments to read log server's endpoint, and few other switches. 
Argument list of LogProbe follows: `[IP:Port] <--noformat> <--bufsize XXXX> <--includeXXX>... <--excludeXXX>...` 
- `--noformat`: Skips formatting of line tags. (Such as 001 to GAMELOG, or 002 to RENLOG) 
- `--bufsize [number]`: Size of the read buffer. Default is 1024. (If you are having too many "Failed to parse tag for line" errors, try increasing this)
- `--include[number]`: Includes specified tag. (Adding at least 1 inclusion will exclude all lines, except for included ones.)
- `--exclude[number]`: Excludes specified tag. (Adding at least 1 inclusion will override all exclusions defined with this switch.) 

*Arguments in [square brackets] are required, arguments in \<angle brackets\> are optional, arguments ending with ... can be used multiple times.*  
**Example**: `LogProbe.exe 127.0.0.1:7025 --bufsize 2048 --include003 --include999` *(This example prints only console outputs, and a custom type tagged 999 with 2 KBs of buffer size.)*  

## License?
LogProbe is not licensed as it doesn't take too much creativity and effort to do such a thing. You can do whatever you want with it, but I'd be happy if I am credited for modified versions.
