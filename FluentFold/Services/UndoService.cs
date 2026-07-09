using System;
using System.Threading.Tasks;
using Windows.Storage;
using FluentFold.Models;

namespace FluentFold.Services;

public class UndoService
{
    private readonly OrganizerService _organizer;

    public UndoService(OrganizerService organizer)
    {
        _organizer = organizer;
    }

    public async Task<bool> UndoAsync()
    {
        return await _organizer.UndoLastAsync();
    }
}