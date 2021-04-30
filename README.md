# <img src="AFK-Script-Interpreter\icon.png" style="width:32px;" />  AFK Script
 A minimalist instruction language for automating user input at specified times

### About

A language developed by me.

| Description    | Values |
| -------------- | ------ |
| File extension | .afk   |

### Command-Line Options

| Flag | Description                                 |
| ---- | ------------------------------------------- |
| /cp  | Run scripts as **parallel** child processes |



### INSTRUCTION SET

Mandatory parameters are specified with "[...]" and optional with "(..)".

| Instruction | Parameter(s)          | Description                                                  |
| ----------- | --------------------- | ------------------------------------------------------------ |
| AT          | (date) (time)         | Stops the program flow until the specified time has passed   |
| WAIT        | [milliseconds]        | Waiting a certain number of **milliseconds**                 |
| CLICK       | [x] [y]               | Performs a mouse click event on a given coordinate on the screen |
| KEY         | [key] (key) (key) ... | performs a keystroke event of a single or a number of key combinations. |
| TYPE        | [text]                | **Sends** a specified character string **to standard input** or at the current text cursor |
| LOG         | [text]                | Write to the standard output (or console)                    |
| READ        | [variable]            | Pauses program flow and **read from standard-input** and store in variable. Can be time, date or text |
| START       | [program] (params)    | Starts a new process similar to using the command prompt     |
| STOP        | [program]             | Stops a process if it is running                             |
| PAUSE       |                       | Pause program and wait for user interaction                  |
| SET         | [variable] [text]     | Store a text value in variable.                              |
| #           | (comment)             | Everything to the right of the hash-tag will be commented out |

### Formatting

| Parameter    | Format                                 | Examples                            | parameters significance                                      |
| ------------ | -------------------------------------- | ----------------------------------- | ------------------------------------------------------------ |
| date         | (MM)/(dd)/(yyyy)<br />(yyyy)-(MM)-(dd) | 05/29/2019, 2020-01-31, 02/14 or 04 | 3: Exact date<br />2: Exact day and month every year<br />1: Exact day every month and every year |
| time         | (HH):(mm):(ss)                         | 05:50:06, 12:32 or 09               | 3: Exact time any day<br />2: Hour and minutes<br />1: Hour  |
| variable     | $[name]                                | $shutdownTime, $repeat or $url      |                                                              |
| milliseconds | [ammount]                              | 1000, 500                           |                                                              |
| x            | [integer]                              | 405, 316                            |                                                              |
| y            | [integer]                              | 231, 675                            |                                                              |
| key          | [key name]                             | ENTER, Cancel, F6 or H              |                                                              |
| text         | "[string]"                             | "Hello, world!"                     |                                                              |
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

