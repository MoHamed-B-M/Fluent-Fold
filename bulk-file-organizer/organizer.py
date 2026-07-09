import os
import shutil
from pathlib import Path
from datetime import datetime
import json

class FileOrganizer:
    def __init__(self):
        self.file_types = {
            'Images': ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.svg', '.ico', '.webp'],
            'Documents': ['.pdf', '.doc', '.docx', '.txt', '.xls', '.xlsx', '.ppt', '.pptx', '.md', '.csv', '.json', '.xml', '.html', '.css', '.js'],
            'Videos': ['.mp4', '.avi', '.mkv', '.mov', '.wmv', '.flv', '.webm'],
            'Audio': ['.mp3', '.wav', '.flac', '.aac', '.ogg', '.m4a'],
            'Archives': ['.zip', '.rar', '.7z', '.tar', '.gz', '.bz2', '.iso'],
            'Code': ['.py', '.java', '.cpp', '.c', '.h', '.js', '.ts', '.go', '.rs', '.rb', '.php', '.sql', '.sh', '.bat', '.ps1']
        }
        self.undo_history = []
        self.rename_history = []

    def organize_by_type(self, folder_path):
        folder_path = Path(folder_path)
        if not folder_path.exists():
            return {'error': 'Folder does not exist'}

        for category in self.file_types.keys():
            (folder_path / category).mkdir(exist_ok=True)

        organized_files = {'moved': [], 'total': 0}

        for item in folder_path.iterdir():
            if item.is_file():
                extension = item.suffix.lower()
                moved = False

                for category, extensions in self.file_types.items():
                    if extension in extensions:
                        destination = folder_path / category / item.name
                        if destination.exists():
                            destination = self._handle_duplicate(destination)
                        shutil.move(str(item), str(destination))
                        self.undo_history.append({'original': str(item), 'destination': str(destination)})
                        organized_files['moved'].append({'from': item.name, 'to': str(destination)})
                        organized_files['total'] += 1
                        moved = True
                        break

                if not moved:
                    others_folder = folder_path / 'Others'
                    others_folder.mkdir(exist_ok=True)
                    destination = others_folder / item.name
                    if destination.exists():
                        destination = self._handle_duplicate(destination)
                    shutil.move(str(item), str(destination))
                    self.undo_history.append({'original': str(item), 'destination': str(destination)})
                    organized_files['moved'].append({'from': item.name, 'to': str(destination)})
                    organized_files['total'] += 1

        return organized_files

    def rename_files(self, folder_path, pattern, start_num=1):
        folder_path = Path(folder_path)
        if not folder_path.exists():
            return {'error': 'Folder does not exist'}

        renamed_files = {'renamed': [], 'total': 0}
        files = [f for f in folder_path.iterdir() if f.is_file()]
        files.sort()

        for index, file_path in enumerate(files, start=start_num):
            extension = file_path.suffix
            padding = len(str(len(files) + start_num - 1))
            new_name = f"{pattern}_{str(index).zfill(padding)}{extension}"
            new_path = folder_path / new_name

            if new_path.exists():
                new_name = f"{pattern}_{str(index).zfill(padding)}_copy{extension}"
                new_path = folder_path / new_name

            file_path.rename(new_path)
            self.rename_history.append({'original': str(file_path), 'new': str(new_path)})
            renamed_files['renamed'].append({'from': file_path.name, 'to': new_name})
            renamed_files['total'] += 1

        return renamed_files

    def _handle_duplicate(self, destination):
        destination = Path(destination)
        counter = 1
        while destination.exists():
            new_name = f"{destination.stem}_{counter}{destination.suffix}"
            destination = destination.parent / new_name
            counter += 1
        return destination

    def undo_last_operation(self):
        if self.undo_history:
            parents = set()
            for action in self.undo_history:
                original = Path(action['original'])
                destination = Path(action['destination'])
                if destination.exists():
                    shutil.move(str(destination), str(original))
                    parents.add(destination.parent)
            for parent in parents:
                if parent.exists() and not any(parent.iterdir()):
                    try:
                        parent.rmdir()
                    except OSError:
                        pass
            self.undo_history.clear()
            return {'success': True, 'message': 'Undo successful'}
        elif self.rename_history:
            for action in reversed(self.rename_history):
                original = Path(action['original'])
                new = Path(action['new'])
                if new.exists():
                    new.rename(original)
            self.rename_history.clear()
            return {'success': True, 'message': 'Undo successful'}
        else:
            return {'success': False, 'message': 'Nothing to undo'}

    def get_folder_summary(self, folder_path):
        folder_path = Path(folder_path)
        if not folder_path.exists():
            return {'error': 'Folder does not exist'}

        summary = {'total_files': 0, 'total_folders': 0, 'categories': {}}

        for item in folder_path.iterdir():
            if item.is_file():
                summary['total_files'] += 1
                ext = item.suffix.lower()
                categorized = False
                for category, extensions in self.file_types.items():
                    if ext in extensions:
                        summary['categories'][category] = summary['categories'].get(category, 0) + 1
                        categorized = True
                        break
                if not categorized:
                    summary['categories']['Others'] = summary['categories'].get('Others', 0) + 1
            elif item.is_dir():
                summary['total_folders'] += 1

        return summary