using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;

namespace VeterinerKlinigi.Web.Areas.Veterinarian.Controllers;

/// <summary>Veteriner: yazdığı muayene notları ve düzenleme.</summary>
public class ExaminationNotesController : VeterinarianAreaController
{
    private readonly IExaminationNoteService _noteService;

    public ExaminationNotesController(IExaminationNoteService noteService)
    {
        _noteService = noteService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _noteService.GetListAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await _noteService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var note = result.Data!;
        ViewData["Note"] = note;
        return View(new ExaminationNoteSaveDto { Diagnosis = note.Diagnosis, Treatment = note.Treatment, Notes = note.Notes });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ExaminationNoteSaveDto model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _noteService.UpdateAsync(id, model, cancellationToken);
            if (result.IsSuccess)
            {
                ShowSuccess(result.Message);
                return RedirectToAction(nameof(Index));
            }

            if (result.ErrorType is ErrorType.NotFound or ErrorType.Forbidden)
            {
                return HandleFailure(result);
            }

            ModelState.AddResultError(result);
        }

        var note = await _noteService.GetByIdAsync(id, cancellationToken);
        if (!note.IsSuccess)
        {
            return HandleFailure(note);
        }

        ViewData["Note"] = note.Data;
        return View(model);
    }
}
