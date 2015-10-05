# rateup
rateup is a command line driven rate card transformation tool that allows you to take a supplier rate card and generate client rate cards based on different markup types.

rateup supports rating based on classes (e.g. Mobiles/Local/National Rate) using dial pattern prefixes and varying markup types which can all be defined in the configuration file.

*rateup is currently a work in progress!*
### Usage
```
rateup /i <input csv file> /o <output csv file> /c <config file>
```
We can add ```/v``` to make rateup output verbose information. For example:
```
12345 rerated with class Default Worldwide Class from 0.833300 to 101.6600
```

### Classes
A class denotes a call type, e.g. Mobile. Each class is used to decide how much the value of the rate should be transformed by.
```json
{
    "Name": "Mobile Rates",
    "Pattern": "^447[0-9]",
    "Priority":  1,
    "Expressions": [
        {
            "Name": "PercentageMarkup",
            "Param":  "1.5"
        }
    ]
}
```

### Expressions
Expressions are used to define different ways of tranforming the markup value. Multiple Expressions can be defined and support functions such as rounding, floor, ceiling etc.

By default an expression is passed 'x', the value of the column to be rated. Value 'y' is passed in as a result of the class's Palam value. This allows you to pass in a percentage value for example varied by the class.
```json
{
    "Name": "PercentageMarkup",
    "Expression": "x*y"
}
```
In this example we use an Expression to transform a rate by rounding its value to 4 decimal places, then converting it from pounds to pence (GBP to Pence).
```json
{
    "Name": "PercentageMarkup",
    "Expression": "m.Round(x*y, 4)*100"
}
```
