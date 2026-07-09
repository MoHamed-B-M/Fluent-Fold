import sys
import importlib.abc
import importlib.machinery
import types


class QRegExpStub:
    pass


class QRegExpValidatorStub:
    pass


_EXTRA_QT_CORE = {
    'pyqtSignal': None,
    'QRegExp': QRegExpStub,
}
_EXTRA_QT_GUI = {
    'QRegExpValidator': QRegExpValidatorStub,
}


def _create_stub_module(name):
    mod = types.ModuleType(name)
    mod.__package__ = name.rpartition('.')[0] if '.' in name else name
    mod.__path__ = []
    return mod


class PyQt5ShimFinder(importlib.abc.MetaPathFinder):

    def find_spec(self, fullname, path, target=None):
        if fullname == 'PyQt5':
            return importlib.machinery.ModuleSpec(
                fullname, None, is_package=True)

        prefix = 'PyQt5.'
        if not fullname.startswith(prefix):
            return None

        sub = fullname[len(prefix):]
        pyside_name = f'PySide6.{sub}'

        try:
            target_mod = importlib.import_module(pyside_name)
        except (ImportError, ModuleNotFoundError):
            target_mod = None

        loader = _ShimLoader(fullname, target_mod)
        return importlib.machinery.ModuleSpec(fullname, loader, is_package=False)


class _ShimLoader(importlib.abc.Loader):

    def __init__(self, shim_name, target_mod):
        self.shim_name = shim_name
        self.target_mod = target_mod

    def create_module(self, spec):
        return None

    def exec_module(self, module):
        if self.target_mod is not None:
            for attr in dir(self.target_mod):
                if not attr.startswith('_'):
                    try:
                        setattr(module, attr, getattr(self.target_mod, attr))
                    except Exception:
                        pass

        if self.shim_name == 'PyQt5.QtCore':
            module.pyqtSignal = module.Signal
            for name, val in _EXTRA_QT_CORE.items():
                if not hasattr(module, name):
                    setattr(module, name, val)
        elif self.shim_name == 'PyQt5.QtGui':
            for name, val in _EXTRA_QT_GUI.items():
                if not hasattr(module, name):
                    setattr(module, name, val)


sys.meta_path.insert(0, PyQt5ShimFinder())
