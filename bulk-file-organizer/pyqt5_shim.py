import sys
import importlib.abc
import importlib.machinery


class PyQt5ShimFinder(importlib.abc.MetaPathFinder):
    _MAPPING = {
        'PyQt5': None,
        'PyQt5.QtCore': 'PySide6.QtCore',
        'PyQt5.QtGui': 'PySide6.QtGui',
        'PyQt5.QtWidgets': 'PySide6.QtWidgets',
    }
    _REEXPORT = {'Qt', 'Signal', 'QPoint', 'QSize', 'QColor', 'QBrush',
                 'QPixmap', 'QPainter', 'QPen', 'QIcon', 'QApplication',
                 'QLabel', 'QWidget', 'QPushButton', 'QFrame', 'QVBoxLayout'}

    def find_spec(self, fullname, path, target=None):
        if fullname not in self._MAPPING:
            return None
        target_name = self._MAPPING[fullname]
        if target_name is None:
            loader = importlib.machinery.ModuleSpec(
                fullname, None, is_package=True)
            return importlib.machinery.ModuleSpec(fullname, None, is_package=True)
        loader = _ShimLoader(fullname, target_name)
        return importlib.machinery.ModuleSpec(fullname, loader, is_package=False)


class _ShimLoader(importlib.abc.Loader):
    def __init__(self, shim_name, target_name):
        self.shim_name = shim_name
        self.target_name = target_name

    def create_module(self, spec):
        return None

    def exec_module(self, module):
        target = importlib.import_module(self.target_name)
        for attr in dir(target):
            if not attr.startswith('_'):
                setattr(module, attr, getattr(target, attr))


sys.meta_path.insert(0, PyQt5ShimFinder())
