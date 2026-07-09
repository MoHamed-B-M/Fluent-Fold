import sys
import importlib.abc
import importlib.machinery


class QRegExpStub:
    pass


class QRegExpValidatorStub:
    pass


class PyQt5ShimFinder(importlib.abc.MetaPathFinder):
    _MAPPING = {
        'PyQt5': None,
        'PyQt5.QtCore': 'PySide6.QtCore',
        'PyQt5.QtGui': 'PySide6.QtGui',
        'PyQt5.QtWidgets': 'PySide6.QtWidgets',
    }

    def find_spec(self, fullname, path, target=None):
        if fullname not in self._MAPPING:
            return None
        target_name = self._MAPPING[fullname]
        if target_name is None:
            return importlib.machinery.ModuleSpec(fullname, None, is_package=True)
        loader = _ShimLoader(fullname, target_name)
        return importlib.machinery.ModuleSpec(fullname, loader, is_package=False)


class _ShimLoader(importlib.abc.Loader):
    _SHIM_ATTRS = {
        'PyQt5.QtCore': {
            'pyqtSignal': None,
            'QRegExp': QRegExpStub,
        },
        'PyQt5.QtGui': {
            'QRegExpValidator': QRegExpValidatorStub,
        },
        'PyQt5.QtWidgets': {},
    }

    def __init__(self, shim_name, target_name):
        self.shim_name = shim_name
        self.target_name = target_name

    def create_module(self, spec):
        return None

    def exec_module(self, module):
        target = importlib.import_module(self.target_name)
        for attr in dir(target):
            if not attr.startswith('_'):
                try:
                    setattr(module, attr, getattr(target, attr))
                except Exception:
                    pass
        if self.shim_name == 'PyQt5.QtCore':
            module.pyqtSignal = module.Signal
        for name, val in self._SHIM_ATTRS.get(self.shim_name, {}).items():
            if not hasattr(module, name):
                setattr(module, name, val)


sys.meta_path.insert(0, PyQt5ShimFinder())
