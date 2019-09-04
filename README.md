# dockercmd

A helper to run Docker containers as commands.

Loosely inspired by Jessie Frazelle's blog post ( https://blog.jessfraz.com/post/docker-containers-on-the-desktop/ ) on the subject of running desktop apps in Docker containers, and some experiments of my own in that direction, dockercmd exists to simplify this process. It uses configuration files to define the run-time configuration of "command containers", letting you invoke them as simple commands.

## Installation

Simply copy the files in the appropriate release .zip into a directory on your path.

Since `dockercmd` is a relatively long command, I suggest adding a shorter alias for it; `dodo`, by analogy to `sudo`, or even simply `d`.

You may also wish to set the `DOCKER_REPO_PREFIX` environment variable. This appends the repository named in the variable to all image names used in command definition files (see below) for which a repository isn't explicitly specified; i.e., if you set `DOCKER_REPO_PREFIX` to `cerebrate` then the example configuration file below will actually use the image `cerebrate/czsh`. This is obviously useful if you store your command images in a repo. `dockercmd` will also automatically pull command images if it doesn't find them locally, which requires this to be set.

## Configuration

`dockercmd` is configured by JSON-formatted command definition files, located in the `.dockercmd` subdirectory of, on Windows, your user profile (i.e., `C:\Users\username`) or on Linux your home directory. These files are named after the command _as you type it_ , which is does not need to be the same as the image name, followed by .json. For example, a configuration file for the `czsh` command would be named `czsh.json`, and might read as follows:

```
{
    "image" : "czsh",
    "name" : "czsh-cont",
    "interactive" : true,
    "persistContainer" : false,
    "publishTcpPorts" : [ 80, 443 ],
    "mountCwd" : "/host",
    "shareHostPids" : false
}
```

**NOTE: This example is modified to show off all the configuration options; it should not be used as-is.**

The options that can be included in the configuration file are as follows:

  * **image** - the name of the image that will be run in a container to execute the command. If not qualified with a repository (i.e., `cerebrate/czsh` rather than `czsh`), the repository specified in the `DOCKER_REPO_PREFIX` environment variable will be automatically prepended, if set. This is the only mandatory option.
  * **name** - the optional name of the container used to execute the command. If this parameter is specified, the container will be named as specified, with the pid of the `dockercmd` process appended to prevent name collisions. If this parameter is not specified, the container name defaults to _command_imagename_pid_ .
  * **interactive** - true if the command is interactive, false if it is not. Interactive commands are run with an interactive pseudo-tty (corresponding to `docker run` options `-it`); non-interactive commands are run detached and with a mini-init (corresponding to `docker run` options `-d --init`). If not specified, defaults to true.
  * **persistContainer** - ordinarily, `dockercmd` automatically deletes command containers when they exit (i.e., uses the `--rm` parameter to `docker run`). Setting this parameter to true leaves the container in place after it exists; intended for debugging purposes.
  * **publishTcpPorts** - an optional array of TCP port numbers to publish to the host computer, corresponding to those exposed by the container. Ports specified in this list are published on the host using the same port number as that exposed.
  * **mountCwd** - an optional volume mount point in the container on which the current working directory will be mounted when the command is executed.
  * **shareHostPids** - setting this parameter to true causes the container to share the host's pid namespace, as if `--pid host` were specified to `docker run`.

Example command definition files are available in the `examples` directory of this repository, although since they reference my images, they won't work for you unless you set `DOCKER_REPO_PREFIX` to `cerebrate`, or edit the image names to include `cerebrate/`.

## Invocation

`dockercmd <command> <arguments>`

or, with the aliases,

`dodo <command> <arguments>`

It's as simple as that!

### Return codes

The return code from `dockercmd` is, as with `docker run`, the return code of the container if interactive, and 0 if non-interactive. Errors in dockercmd are specified using return codes > 128, as follows:

  * **129** - command-line arguments invalid
  * **130** - could not find command definition file
  * **131** - malformed command definition file
  * **132** - could not pull missing command image
  * **255** - unanticipated error

## Compensation

If you find `dockercmd` useful, I'd surely appreciate it if you'd throw a buck or two my way. Your contributions help pay for shiny toys, the continuing attrition of my liver, and most relevantly, future feature development!

[![ko-fi](https://www.ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/I3I1VA18)

## Forthcoming

Here's what's currently on my roadmap:

   * Packaging (for Windows, with Chocolatey; for Linux, for apt).
   * Publishing of non-TCP ports.
   * Publish all exposed ports on random ports.
   * More port publishing options.
   * More volume mapping options.
   * Container environment variables.
   * and more...
 
Of course, I'll be adding these things as I have need of them, so there's no time-frame on that. If there's something else you need, feel free to let me know by raising an issue, or even better - and likely to be substantially faster - submitting a pull request.

...
