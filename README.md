# <img src="AFK-Script-Interpreter\icon.png" style="width:25%;" />  AFK Script
 A minimalist instruction language for automating user input at specified times

### About

| Description    | Values |
| -------------- | ------ |
| File extension | .afk   |

### INSTRUCTION SET

Mandatory parameters are specified with "[...]" and optional with "(..)".

| Instruction | Parameter(s)                | Description                                                  |
| ----------- | --------------------------- | ------------------------------------------------------------ |
| AT          | (date), (time)              | Stops the program flow until the specified time has passed   |
| WAIT        | [milliseconds]              | Waiting a certain number of milliseconds                     |
| CLICK       | [x], [y]                    | Performs a mouse click event on a given coordinate on the screen |
| KEY         | [key] (key) (key) ...       | performs a keystroke event of a single or a number of key combinations. |
| TYPE        | "[text]"                    | Sends the specified character string to standard input or at the current text cursor |
| START       | [program path] (cmd params) | Starts a new process similar to using the command prompt     |

### Examples

#### 1)

```d
AT 17:18
CLICK 532, 122
CLICK 140, 60
WAIT 10
KEY ENTER

AT 17:45
START "" "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
```

This script will (when executed) wait until the current time has passed 17:18:00 today (default)
and then preform two click instructions on the screen, wait 10 seconds, send
that the key enter was pressed. Wait until 17:45:00 has passed, open up the default browser with a predetermined URL, and then terminate.
