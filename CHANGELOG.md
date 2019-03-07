# Changelog

## 2.0.0-a1

* Test release

## 2.0.0-b01

* Test release

## 2.0.0

* Release 2.0.0

## 3.0.0

* Update to Rebus 3

## 4.0.0

* Update to Rebus 4
* Port to new project structure

## 4.0.1

* Fix exception-during-disposal bug - thanks [nebelx]

## 4.1.0

* Add ability to configure position of transaction scope step in the incoming pipeline - thanks [larsw]

## 4.1.1

* Add .NET Standard 2.0 as a target in addition to .NET 4.5.1

## 5.0.0-b1

* Change it so that the receive operation is enlisted in the `TransactionScope` too, when "handle messages inside TransactionScope" is enabled

[larsw]: https://github.com/larsw
[nebelx]: https://github.com/nebelx
