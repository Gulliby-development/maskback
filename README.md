<p align="center"><img src="docs/logo.svg" width="96" alt="" /></p>

# maskback

maskback is an offline command-line tool for people who want to inspect and locally mask sensitive text before sending text to a language model. It is written in C# for .NET 10 and has no external runtime package dependency. Its `mask`, `scan`, `unmask`, and `relay` commands work with email addresses, card numbers, IBANs, IPv4 addresses, and selected secret-key formats. Masking substitutes numbered placeholders such as `{{EMAIL_1}}`; a local map can later restore the originals.

The included `relay` command is a deterministic, local privacy demonstration, not a connection to a language model. It uses the `Scripted` fake provider and a reply script, so it needs no network, API key, model account, or service. It records the conversations given to that fake provider so you can inspect what was sent.

[![CI](https://github.com/Gulliby-development/maskback/actions/workflows/ci.yml/badge.svg)](https://github.com/Gulliby-development/maskback/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Requirements and build

maskback is intended to run on Windows, macOS, and Linux. Install the .NET 10 SDK from [Microsoft's .NET downloads](https://dotnet.microsoft.com/download/dotnet/10.0), then get the project and build it:

```sh
git clone https://github.com/Gulliby-development/maskback.git
cd maskback
dotnet build
dotnet run --project src/Maskback -- --help
```

The transcript below uses `maskback` as shorthand for the built application. No global installation is assumed.

## Commands and behavior

The command synopsis is:

```text
usage: maskback mask FILE [--kinds LIST] [--map FILE]
       maskback scan FILE [--kinds LIST] [--summary]
       maskback unmask FILE --map FILE [--strict]
       maskback relay FILE... --script FILE [--kinds LIST] [--sent FILE]
kinds: email, key, iban, card, ipv4
```

The four commands are:

- `mask FILE [--kinds LIST] [--map FILE]` reads a text file (or standard input when `FILE` is `-`), replaces every selected finding with a numbered placeholder, and writes the masked text to standard output. If `--map FILE` is supplied, it also writes the placeholder-to-original map to that path.
- `scan FILE [--kinds LIST] [--summary]` prints one `LINE:COL KIND ORIGINAL` row per finding (with `--summary`, one `KIND COUNT` row per selected kind) and then `maskback: N findings`. Its exit code is 1 when it finds anything and 0 otherwise. Lines count LF characters; columns are one-based UTF-16 code-unit offsets from the start of the line, and a CR counts as a column.
- `unmask FILE --map FILE [--strict]` reads the map and replaces recognized placeholders in the reply. Unknown placeholders remain unchanged and are reported to standard error. `--strict` makes the exit code 1 if any unknown placeholder remains.
- `relay FILE... --script FILE [--kinds LIST] [--sent FILE]` masks one or more prompts with a shared masker, sends each prompt and the accumulated conversation to the local `Scripted` provider, restores the returned reply using mappings made so far, and prints the replies separated by `---` and LF. `--sent FILE` writes the provider conversation log.

The kinds are `email`, `key`, `iban`, `card`, and `ipv4`; by default all five are enabled. `--kinds` accepts a comma-separated list of these names. Options can be written as `--name value` or `--name=value`; `--strict` is a switch and takes no value. A standalone `--` ends option parsing. A single hyphen as a file name means standard input. `help` or `--help` prints usage, and `--version` prints the version.

`scan --summary` is a switch for a quick overview of a file: instead of one row per finding it prints one `KIND COUNT` row for each selected kind, always in the order `email`, `key`, `iban`, `card`, `ipv4` whatever the order of `--kinds`, with `0` for a kind that was not found, and then the usual `maskback: N findings` line. The exit code is the same as without it: 1 when anything was found and 0 otherwise.

Input text is decoded as strict UTF-8: malformed input is an error, and one leading U+FEFF is removed by the text decoder. The map parser still requires its exact `maskback-map 1` header. Findings are whole words: runs are split on space, TAB, CR, and LF, then surrounding punctuation is trimmed. Detectors use ASCII character classes and their documented formats; this is not general-purpose data-loss prevention.

Errors are written to standard error as `maskback: MESSAGE` followed by LF. Exit code 0 means success, 1 indicates a finding, strict unknown placeholder, or exhausted script, and 2 indicates a usage or input error (including invalid UTF-8, unreadable input, an invalid map, unknown kind, or an unknown option). With no arguments, usage goes to standard error and the exit code is 2; `help` and `--help` print usage to standard output, and `--version` prints `maskback 0.1.0` plus LF. An unknown first command reports `unknown command X`. An ordinary unmask or relay with unknown placeholders reports each one without making it an error exit. Other option errors include `unknown option --x`, `option --x needs a value`, `option --x given twice`, and `option --strict takes no value`; an incorrect positional count reports the corresponding command usage. An unmask map must begin with `maskback-map 1`; malformed or duplicate map entries are input errors.

Validation happens before reads where possible: kinds are checked before any file is read. `unmask` checks the positional count before requiring `--map`, then reads the map before the reply. `relay` requires at least one prompt and `--script`, checks kinds, reads the script, and then reads every prompt before making any provider call. This ensures that a missing or invalid prompt cannot leave a partially produced relay transcript. A command failure queues no output-file writes; this is not a promise of transactional protection if the operating system or filesystem fails during a later file write.

## Worked examples

To try the examples in a POSIX shell (on Windows use Git Bash), build the project, then run this in the project directory; it defines the `maskback` shorthand, moves to a scratch directory and creates the input files (`in.txt`, the reply `r.txt`, the prompts `p0.txt` and `p1.txt`, the two-reply script `s.txt` and the one-reply script `one.txt`; the script separator is a line with exactly three hyphens):

```sh
MASKBACK_DLL="$PWD/src/Maskback/bin/Debug/net10.0/Maskback.dll"
maskback() { dotnet "$MASKBACK_DLL" "$@"; }
cd "$(mktemp -d)"
printf 'Mail alice@example.com or (bob@mail.example.org).\nCard 4111-1111-1111-1111, pay GB82WEST12345698765432 from 192.168.1.10.\nkey sk-abcdefghijklmnopqrstuvwxyz and AKIAABCDEFGHIJKLMNOP, again alice@example.com\n' > in.txt
printf 'Dear {{EMAIL_1}}, your card {{CARD_1}} is fine; {{EMAIL_7}} is not known.\n' > r.txt
printf 'Please email alice@example.com about card 4111-1111-1111-1111.\n' > p0.txt
printf 'Also tell bob@mail.example.org and alice@example.com.\n' > p1.txt
printf 'Sure, I will write to {{EMAIL_1}}.\n---\nDone: {{EMAIL_2}} and {{EMAIL_1}} know; {{CARD_1}} is noted; {{EMAIL_3}}?\n' > s.txt
printf 'one reply\n' > one.txt
```

The transcript below is the output of these commands, run in that order. `stderr:` and `exit:` label standard-error output and a non-zero process status. Each line of `m.txt` holds the placeholder, one TAB and the original; the `\n` in `log.txt` is the two-character escape of an LF inside a message. The standard-input example sends the bytes `61 40 62 2E 63 6F 0A FF 0A`, which are not valid UTF-8; the last example runs the two prompts against a script with one reply.

```text
$ maskback scan in.txt
1:6 email alice@example.com
1:28 email bob@mail.example.org
2:6 card 4111-1111-1111-1111
2:31 iban GB82WEST12345698765432
2:59 ipv4 192.168.1.10
3:5 key sk-abcdefghijklmnopqrstuvwxyz
3:39 key AKIAABCDEFGHIJKLMNOP
3:67 email alice@example.com
maskback: 8 findings
exit: 1
$ maskback scan in.txt --summary
email 3
key 2
iban 1
card 1
ipv4 1
maskback: 8 findings
exit: 1
$ maskback mask in.txt --map m.txt
Mail {{EMAIL_1}} or ({{EMAIL_2}}).
Card {{CARD_1}}, pay {{IBAN_1}} from {{IPV4_1}}.
key {{KEY_1}} and {{KEY_2}}, again {{EMAIL_1}}
$ cat m.txt
maskback-map 1
{{EMAIL_1}}	alice@example.com
{{EMAIL_2}}	bob@mail.example.org
{{CARD_1}}	4111-1111-1111-1111
{{IBAN_1}}	GB82WEST12345698765432
{{IPV4_1}}	192.168.1.10
{{KEY_1}}	sk-abcdefghijklmnopqrstuvwxyz
{{KEY_2}}	AKIAABCDEFGHIJKLMNOP
$ maskback unmask r.txt --map m.txt
Dear alice@example.com, your card 4111-1111-1111-1111 is fine; {{EMAIL_7}} is not known.
stderr: maskback: unknown placeholder {{EMAIL_7}}
$ maskback unmask r.txt --map m.txt --strict
Dear alice@example.com, your card 4111-1111-1111-1111 is fine; {{EMAIL_7}} is not known.
stderr: maskback: unknown placeholder {{EMAIL_7}}
exit: 1
$ maskback mask in.txt --kinds email,ipv4
Mail {{EMAIL_1}} or ({{EMAIL_2}}).
Card 4111-1111-1111-1111, pay GB82WEST12345698765432 from {{IPV4_1}}.
key sk-abcdefghijklmnopqrstuvwxyz and AKIAABCDEFGHIJKLMNOP, again {{EMAIL_1}}
$ maskback relay p0.txt p1.txt --script s.txt --sent log.txt
Sure, I will write to alice@example.com.
---
Done: bob@mail.example.org and alice@example.com know; 4111-1111-1111-1111 is noted; {{EMAIL_3}}?
stderr: maskback: unknown placeholder {{EMAIL_3}}
$ cat log.txt
call 1
user Please email {{EMAIL_1}} about card {{CARD_1}}.\n
call 2
user Please email {{EMAIL_1}} about card {{CARD_1}}.\n
assistant Sure, I will write to {{EMAIL_1}}.\n
user Also tell {{EMAIL_2}} and {{EMAIL_1}}.\n
$ printf 'a@b.co\n\377\n' | maskback scan -
stderr: maskback: <stdin>: input is not valid UTF-8
exit: 2
$ maskback mask missing.txt
stderr: maskback: cannot read missing.txt
exit: 2
$ maskback mask in.txt --kinds phone
stderr: maskback: unknown kind phone
exit: 2
$ maskback relay p0.txt p1.txt --script one.txt
stderr: maskback: script has no reply for call 2
exit: 1
```

## Scripted relay and privacy

A script is split into replies at lines that are exactly `---`. Each reply is its lines joined with LF and a final LF. A blank line is a line, but a reply with no lines is an error; an empty script is one empty reply and also fails. A final empty reply after a trailing separator is dropped.

For each prompt, relay masks the user text before sending it to `Scripted`. It reuses a single masker across prompts, so an already-seen (kind, original) pair gets the same placeholder and new values get increasing numbers per kind. The provider receives the full conversation: previous masked user prompts and the assistant replies exactly as written in the script. Replies are not re-masked before they enter later conversation history. The returned reply is unmasked using only mappings created up to that point. The `--sent` log labels each call and escapes backslash, LF, and CR so the conversation can be inspected as text.

This is a deterministic local check of the relay's masking behavior, not a guarantee that arbitrary text is safe to send elsewhere. Detection is limited to the selected kinds and the ASCII formats recognized by the detectors. It can miss sensitive values in unsupported formats or embedded in a token the word rules do not isolate, and a matching non-sensitive value can be a false positive. Unselected kinds remain visible. The map contains the plaintext originals; store it carefully and remove it when no longer needed. Inspect prompts, replies, maps, and logs before using any output outside the local demonstration.

## Development

Run these checks from the repository root:

```sh
dotnet build
dotnet format --verify-no-changes
dotnet test
```

The build treats analyzer warnings as errors.

## License

MIT, see [LICENSE](LICENSE).

---

Built with BAKED
