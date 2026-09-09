import { useState } from "react";
import { api } from "../api/client";
import type { QuestionDto } from "../types";
import AnswerForm from "./AnswerForm";

// Answer/Hide/Unhide/Delete controls for a question's own recipient - used
// both on the Dashboard and inline in the public profile feed, so the owner
// isn't forced to leave their profile page just to answer something.
export default function OwnerControls({ question, onChange }: { question: QuestionDto; onChange: () => void }) {
  const [answering, setAnswering] = useState(false);
  const [busy, setBusy] = useState(false);

  const act = async (action: "hide" | "unhide" | "delete") => {
    setBusy(true);
    try {
      if (action === "delete") await api.delete(`/questions/${question.id}`);
      else await api.patch(`/questions/${question.id}/${action}`);
      onChange();
    } finally {
      setBusy(false);
    }
  };

  return (
    <div onClick={(e) => e.stopPropagation()}>
      <div className="flex flex-wrap gap-2 mt-3">
        <button className="btn-secondary !py-1.5 !px-3 text-xs" disabled={busy}
          onClick={() => setAnswering((a) => !a)}>
          {question.answer ? "Edit answer" : "Answer"}
        </button>
        {question.isVisible ? (
          <button className="btn-secondary !py-1.5 !px-3 text-xs" disabled={busy} onClick={() => act("hide")}>Hide</button>
        ) : (
          <button className="btn-secondary !py-1.5 !px-3 text-xs" disabled={busy} onClick={() => act("unhide")}>Unhide</button>
        )}
        <button className="btn-danger" disabled={busy} onClick={() => act("delete")}>Delete</button>
      </div>

      {answering && (
        <AnswerForm question={question} onDone={() => { setAnswering(false); onChange(); }} />
      )}
    </div>
  );
}
