import { Ticket } from "lucide-react";
import { useState } from "react";
import { useNavigate } from "react-router";
import { TextField } from "@/components/TextField";
import { extractConviteToken } from "./useConvites";

/** Para quem já tem conta e recebeu o link de outro grupo. */
export function ConviteForm() {
  const navigate = useNavigate();
  const [convite, setConvite] = useState("");
  const token = extractConviteToken(convite);

  return (
    <form
      noValidate
      className="card space-y-3 p-5"
      onSubmit={(event) => {
        event.preventDefault();
        if (token) void navigate(`/convite#${token}`);
      }}
    >
      <TextField
        label="Tenho um convite"
        icon={Ticket}
        type="text"
        autoComplete="off"
        placeholder="Cole aqui o link que você recebeu"
        value={convite}
        onChange={(event) => setConvite(event.target.value)}
      />
      <button type="submit" className="btn-secondary w-full" disabled={!token}>
        Continuar
      </button>
    </form>
  );
}
