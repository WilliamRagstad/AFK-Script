# <img src="AFK-Script-Interpreter\icon.png" style="width:32px;" />  AFK Script
 A minimalist instruction language for automating user input at specified times

![Downloads](https://img.shields.io/github/downloads/WilliamRagstad/AFK-Script/total)

### Install

[Download the latest release of AFK Script](https://github.com/WilliamRagstad/AFK-Script/releases/download/v1.0.0/AFKScript.zip).
Unpack the archive on your computer and create a reference in your path environment to it. Now you should be able to run `AFKScript` in the terminal.

### About

A time-based scripting language for automating user tasks.

| Description    | Values |
| -------------- | ------ |
| File extension | .afk   |

### Command-Line Options

| Flag | Description                                 |
| ---- | ------------------------------------------- |
| /cp  | Run scripts as **parallel** child processes |
| /hl  | Hides console log outputs                   |



### Instruction Set

Mandatory parameters are specified with "[...]" and optional with "(...)".

| Instruction | Parameter(s)          | Description                                                  |
| ----------- | --------------------- | ------------------------------------------------------------ |
| AT          | (date) (time)         | Stops the program flow until the specified time has passed   |
| WAIT        | [milliseconds]        | Waiting a certain number of **milliseconds**                 |
| CLICK       |                       | Performs a mouse click event on the current mouse coordinate on the screen |
| CLICK       | [x] [y]               | Performs a mouse click event on a given coordinate on the screen |
| KEY         | [key] (key) (key) ... | performs a keystroke event of a single or a number of key combinations. |
| ~~TYPE~~    | [text]                | **Sends** a specified character string **to standard input** or at the current text cursor |
| LOG         | [text]                | Write to the standard output (or console)                    |
| READ        | [variable]            | Pauses program flow and **read from standard-input** and store in variable. Can be time, date or text |
| START       | [program] (params)    | Starts a new process similar to using the command prompt     |
| ~~STOP~~    | [program]             | Stops a process if it is running                             |
| PAUSE       |                       | Pause program and wait for user interaction                  |
| SET         | [variable] [value]    | Store a value in variable. Value could be text or number.    |
| #           | (comment)             | Everything to the right of the hash-tag will be commented out |

### Formatting

| Parameter    | Format                                 | Examples                            | parameters significance                                      |
| ------------ | -------------------------------------- | ----------------------------------- | ------------------------------------------------------------ |
| date         | (MM)/(dd)/(yyyy)<br />(yyyy)-(MM)-(dd) | 05/29/2019, 2020-01-31, 02/14 or 04 | 3: Exact date<br />2: Exact day and month every year<br />1: Exact day every month and every year |
| time         | (HH):(mm):(ss)                         | 05:50:06, 12:32 or 09               | 3: Exact time any day<br />2: Hour and minutes<br />1: Hour  |
| text         | "[string]"                             | "Hello, world!"                     |                                                              |
| number       | [integer]                              | 42, 100                             |                                                              |
| variable     | $[name]                                | $shutdownTime, $repeat or $url      |                                                              |
| label        | :[name]                                | :start, :end, :L1, :L2              |                                                              |
| milliseconds | *Same as **number***                   | 1000, 500                           |                                                              |
| x            | *Same as **number***                   | 405, 316                            |                                                              |
| y            | *Same as **number***                   | 231, 675                            |                                                              |
| key          | {[key name]} or [text]                 | ENTER, Cancel, F6 or HELLO          | [List or available keys](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=netframework-4.8). |
| program      | *Same as **text***                     |                                     |                                                              |
| params       | *An array of **texts**.*               |                                     |                                                              |

### Wildcards

> ## #
>
> Means that it does not matter which value is on a given position.
> For example: **##:##:00**, will match all time values ending with 00 seconds.
> **12:05:00** or **05:12:00**, but not ~~**14:54:23**~~.

### Environment Variables

| Name  | Description                    | Format     |
| ----- | ------------------------------ | ---------- |
| $TIME | Holds the current machine time | HH:mm:ss   |
| $DATE | Current date                   | yyyy/MM/dd |

### Labels

Create a label on a new row using a prefix `:` with a trailing label name.

```assembly
:start # Create a label start
LOG "HELLO"
GOTO start
```



### Examples

#### 1) First Clicks

```bash
AT 17:18
CLICK 532 122
CLICK 140 60
WAIT 10
KEY ENTER

AT 17:45
START "" "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
```

This script will (when executed) wait until the current time has passed 17:18:00 today (default)
and then preform two click instructions on the screen, wait 10 seconds, send
that the key enter was pressed. Wait until 17:45:00 has passed, open up the default browser with a predetermined URL, and then terminate.

#### 2) Variables

```bash
# Define $URL
$URL = "https://twitter.com/williamragstad"
AT 13:00
START "" $URL
```

This prints the text *"Webpage to open: "* and prompts for a user input which will be stored inside **$URL**. Then waits until 13:00 the same day and opens that URL in the standard browser.

#### 3) Reading Input

```bash
# Read input and store in $URL
LOG "=== WEB OPENER ==="
READ "Webpage: " $URL
AT 13:00
LOG "Opening up $URL"
START "" $URL
```

This prints the text *"Webpage to open: "* and prompts for a user input which will be stored inside **$URL**. Then waits until 13:00 the same day and opens that URL in the standard browser.

