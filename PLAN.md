This repo contains a plugin for Jellyfin media server. I need to make the following improvements

## Configuration Directory
- [x] The config portion of the plugin has javascript written directly into the HTML file, this needs to be placed in a separate file
- [x] I would also like to improve dev workflow for this portion of the code. Right now I need to rebuild the plugin, stage build files in a dev server, and restart the dev server before i can see change
- [x] I would like to make that loop tighter, a small dev server where I can see changes with hot reload so I can get realtime feedback on UI changes.

## Testing
- [ ] Unit tests 
    - [x] I need coverage for Jellyfin.Plugin.CinemaMode/Configuration
    - [ ] I need coverage for Jellyfin.Plugin.CinemaMode/IntroManager.cs
    - [ ] I need coverage for Jellyfin.Plugin.CinemaMode/IntroProvider.cs
    - [ ] Abiility to extensively mock the jellyfin objects needed for test coverage 
        - [ ] ideally with a Factory method for each tpye needing mocking so they can be procedurally generated from text based configurations where differnt vlaues are needed for differnt tests
- [ ] Integration Testing
    - [ ] Use docker containers for integration testing
    - [ ] Mock media directory using empty files and nfos, maybe 
    - [ ] Explore installing the jellyfin libraries and hitting the C# API without a server instance running for CI integration testing. Want to have tests where jellyfins library code is hitting an actual jellyfin db

## Automation and Developer Workflow
- Automated CI actions for testing
     - Unit tests and linting on pull requests requiring manual approval
     - whitelist users to run actions without approval
- Automated CI action for releasing
    - On tag creation
    - run build automation in CI
    - Upload build artifacts as a release
    - Update manifest
- Dev scripts and tasks
    - This will change based on the testing and docker setup but here are some possiblities
    - run a jellyfin dev server in a docker container with the local mock library mounted to a volume
    - mount a local git ignored directory where the build artifacts can be staged
    - run tests
    - run linting and static analyzers
- Cron CI Action
    - Check for new jelllyfin release
    - if new release detected, run CI testing
    - if tests fail create issue
