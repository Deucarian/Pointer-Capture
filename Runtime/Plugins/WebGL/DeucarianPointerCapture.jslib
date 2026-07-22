mergeInto(LibraryManager.library, {
  $deucarianPointerCaptureInstall: function (module) {
    if (module.deucarianPointerCaptureInstalled) {
      return;
    }

    module.deucarianPointerCaptureInstalled = true;
    module.deucarianPointerCaptureStatus = 0;
    module.deucarianPointerCaptureExplicitRelease = false;
    module.deucarianPointerCaptureReleaseOnPageHidden = true;

    document.addEventListener("pointerlockchange", function () {
      if (document.pointerLockElement === module.canvas) {
        module.deucarianPointerCaptureStatus = 2;
        module.deucarianPointerCaptureExplicitRelease = false;
        return;
      }

      if (module.deucarianPointerCaptureExplicitRelease) {
        module.deucarianPointerCaptureStatus = 0;
      } else if (module.deucarianPointerCaptureStatus === 1) {
        module.deucarianPointerCaptureStatus = 3;
      } else if (module.deucarianPointerCaptureStatus === 2) {
        module.deucarianPointerCaptureStatus = 4;
      }

      module.deucarianPointerCaptureExplicitRelease = false;
    });

    document.addEventListener("pointerlockerror", function () {
      module.deucarianPointerCaptureStatus = 3;
      module.deucarianPointerCaptureExplicitRelease = false;
    });

    document.addEventListener("visibilitychange", function () {
      if (!document.hidden || !module.deucarianPointerCaptureReleaseOnPageHidden) {
        return;
      }

      if (module.deucarianPointerCaptureStatus === 1 ||
          module.deucarianPointerCaptureStatus === 2) {
        module.deucarianPointerCaptureStatus = 5;
      }

      if (document.pointerLockElement === module.canvas &&
          typeof document.exitPointerLock === "function") {
        document.exitPointerLock();
      }
    });

    window.addEventListener("blur", function () {
      if (module.deucarianPointerCaptureStatus === 1 ||
          module.deucarianPointerCaptureStatus === 2) {
        module.deucarianPointerCaptureStatus = 4;
      }

      if (document.pointerLockElement === module.canvas &&
          typeof document.exitPointerLock === "function") {
        document.exitPointerLock();
      }
    });
  },

  DeucarianPointerCaptureRequest__deps: ["$deucarianPointerCaptureInstall"],
  DeucarianPointerCaptureRequest: function () {
    if (typeof document === "undefined" || typeof Module === "undefined" || !Module.canvas) {
      return;
    }

    deucarianPointerCaptureInstall(Module);
    var canvas = Module.canvas;
    if (document.pointerLockElement === canvas) {
      Module.deucarianPointerCaptureStatus = 2;
      return;
    }

    if (typeof canvas.requestPointerLock !== "function" ||
        (typeof document.hasFocus === "function" && !document.hasFocus())) {
      Module.deucarianPointerCaptureStatus = 3;
      return;
    }

    Module.deucarianPointerCaptureExplicitRelease = false;
    Module.deucarianPointerCaptureStatus = 1;
    try {
      var request = canvas.requestPointerLock();
      if (request && typeof request.catch === "function") {
        request.catch(function () {
          Module.deucarianPointerCaptureStatus = 3;
        });
      }
    } catch (error) {
      Module.deucarianPointerCaptureStatus = 3;
    }
  },

  DeucarianPointerCaptureRelease__deps: ["$deucarianPointerCaptureInstall"],
  DeucarianPointerCaptureRelease: function () {
    if (typeof document === "undefined" || typeof Module === "undefined") {
      return;
    }

    deucarianPointerCaptureInstall(Module);
    Module.deucarianPointerCaptureExplicitRelease = true;
    Module.deucarianPointerCaptureStatus = 0;
    if (document.pointerLockElement === Module.canvas &&
        typeof document.exitPointerLock === "function") {
      document.exitPointerLock();
    }
  },

  DeucarianPointerCaptureGetStatus__deps: ["$deucarianPointerCaptureInstall"],
  DeucarianPointerCaptureGetStatus: function () {
    if (typeof document === "undefined" || typeof Module === "undefined") {
      return 4;
    }

    deucarianPointerCaptureInstall(Module);
    if (document.pointerLockElement === Module.canvas) {
      Module.deucarianPointerCaptureStatus = 2;
    }

    return Module.deucarianPointerCaptureStatus | 0;
  },

  DeucarianPointerCaptureSetReleaseOnPageHidden__deps: ["$deucarianPointerCaptureInstall"],
  DeucarianPointerCaptureSetReleaseOnPageHidden: function (enabled) {
    if (typeof Module === "undefined") {
      return;
    }

    deucarianPointerCaptureInstall(Module);
    Module.deucarianPointerCaptureReleaseOnPageHidden = enabled !== 0;
  }
});
