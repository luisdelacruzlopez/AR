// ================================================
// SONIDO DE ALERTA AL LLEGAR UNA INCIDENCIA NUEVA
// ================================================
window.playAlertSound = function () {
    try {
        const audio = new Audio("/sounds/alert.mp3");
        audio.volume = 1.0;
        audio.play();
    } catch (err) {
        console.error("No se pudo reproducir el sonido:", err);
    }
};


// ================================================
// DESCARGA DE ARCHIVOS DESDE BLAZOR
// ================================================
window.downloadFile = function (fileName, contentType, data) {
    const blob = new Blob([new Uint8Array(data)], { type: contentType });
    const url = URL.createObjectURL(blob);

    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();

    URL.revokeObjectURL(url);
};


// ================================================
// EXPORTACIÓN DE INCIDENCIAS A PDF  (jsPDF)
// ================================================
window.exportIncidenciasPDF = async function (incidencias, users) {

    if (!window.jspdf) {
        console.error("jsPDF no está cargado.");
        return;
    }

    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    doc.setFontSize(16);
    doc.text("Informe de incidencias", 14, 18);

    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();
    const marginX = 12;
    const tableWidth = pageWidth - (marginX * 2);

    let y = 30;

    const getProp = (obj, ...keys) => {
        if (!obj) return undefined;
        for (const key of keys) {
            if (Object.prototype.hasOwnProperty.call(obj, key)) {
                return obj[key];
            }
        }
        return undefined;
    };

    const usersByUid = new Map((users || []).map(u => {
        const uid = getProp(u, "uid", "Uid");
        return [uid, u];
    }).filter(([uid]) => !!uid));

    const estadoToText = (estado) => {
        if (typeof estado === "number") {
            switch (estado) {
                case 0: return "Pendiente";
                case 1: return "Confirmada";
                case 2: return "En proceso";
                case 3: return "Resuelta";
                default: return "Desconocido";
            }
        }

        if (typeof estado === "string") {
            const norm = estado.trim().toLowerCase();
            if (norm === "pendiente") return "Pendiente";
            if (norm === "confirmada") return "Confirmada";
            if (norm === "enproceso" || norm === "en_proceso" || norm === "en proceso") return "En proceso";
            if (norm === "resuelta") return "Resuelta";
        }

        return String(estado ?? "Desconocido");
    };

    const formatDateTime = (value) => {
        if (!value) return "-";
        const d = new Date(value);
        if (Number.isNaN(d.getTime())) return String(value);
        return d.toLocaleString("es-ES", {
            year: "numeric",
            month: "2-digit",
            day: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit",
            hour12: false
        });
    };

    const getAcomodadorNombre = (uid) => {
        if (!uid) return "Sin asignar";
        const user = usersByUid.get(uid);
        if (!user) return uid;
        const nombre = getProp(user, "nombre", "Nombre");
        return nombre && String(nombre).trim() ? String(nombre) : uid;
    };

    const columns = [
        { key: "tipo", title: "Tipo", width: 20 },
        { key: "zona", title: "Zona", width: 14 },
        { key: "acomodador", title: "Acomodador", width: 30 },
        { key: "estado", title: "Estado", width: 16 },
        { key: "fecha", title: "Fecha", width: 36 },
        { key: "resuelta", title: "Resuelta", width: 36 },
        { key: "tiempoResolucion", title: "T. resol.", width: 24 }
    ];

    const totalUnits = columns.reduce((sum, c) => sum + c.width, 0);
    const colWidths = columns.map(c => (c.width / totalUnits) * tableWidth);
    const rowHeight = 7;

    const drawTableHeader = () => {
        let x = marginX;
        doc.setFillColor(230, 238, 250);
        doc.rect(marginX, y, tableWidth, rowHeight, "F");

        doc.setFont("helvetica", "bold");
        doc.setFontSize(9);

        columns.forEach((col, idx) => {
            doc.rect(x, y, colWidths[idx], rowHeight);
            doc.text(col.title, x + 1.5, y + 4.8);
            x += colWidths[idx];
        });

        y += rowHeight;
        doc.setFont("helvetica", "normal");
    };

    const clipText = (value, maxWidth) => {
        const text = value == null ? "" : String(value);
        if (doc.getTextWidth(text) <= maxWidth) return text;

        let clipped = text;
        while (clipped.length > 0 && doc.getTextWidth(clipped + "...") > maxWidth) {
            clipped = clipped.slice(0, -1);
        }

        return clipped.length ? clipped + "..." : "";
    };

    const parseDate = (value) => {
        if (!value) return null;
        const d = new Date(value);
        return Number.isNaN(d.getTime()) ? null : d;
    };

    const getResolutionTime = (estado, timestamp, horaResolucion) => {
        if (estadoToText(estado) !== "Resuelta") return "-";

        const inicio = parseDate(timestamp);
        const fin = parseDate(horaResolucion);

        if (!inicio || !fin || fin < inicio) return "-";

        const totalSeconds = Math.floor((fin.getTime() - inicio.getTime()) / 1000);
        const days = Math.floor(totalSeconds / 86400);
        const remDay = totalSeconds % 86400;
        const hours = Math.floor(remDay / 3600);
        const remHour = remDay % 3600;
        const minutes = Math.floor(remHour / 60);
        const seconds = remHour % 60;

        const hh = String(hours).padStart(2, "0");
        const mm = String(minutes).padStart(2, "0");
        const ss = String(seconds).padStart(2, "0");

        if (days > 0) return `${days}d ${hh}:${mm}:${ss}`;
        return `${hh}:${mm}:${ss}`;
    };

    drawTableHeader();

    (incidencias || []).forEach((i) => {
        const tipo = getProp(i, "tipo", "Tipo") ?? "";
        const zonaId = getProp(i, "zonaId", "ZonaId") ?? "";
        const acomodadorUid = getProp(i, "acomodadorUid", "AcomodadorUid") ?? "";
        const estado = getProp(i, "estado", "Estado");
        const timestamp = getProp(i, "timestamp", "Timestamp");
        const horaResolucion = getProp(i, "horaResolucion", "HoraResolucion");

        if (y + rowHeight > pageHeight - 12) {
            doc.addPage();
            y = 16;
            drawTableHeader();
        }

        const row = {
            tipo,
            zona: zonaId,
            acomodador: getAcomodadorNombre(acomodadorUid),
            estado: estadoToText(estado),
            fecha: formatDateTime(timestamp),
            resuelta: horaResolucion ? formatDateTime(horaResolucion) : "-",
            tiempoResolucion: getResolutionTime(estado, timestamp, horaResolucion)
        };

        let x = marginX;
        doc.setFontSize(8.5);

        columns.forEach((col, idx) => {
            doc.rect(x, y, colWidths[idx], rowHeight);
            const safeText = clipText(row[col.key], colWidths[idx] - 3);
            doc.text(safeText, x + 1.5, y + 4.8);
            x += colWidths[idx];
        });

        y += rowHeight;
    });

    doc.save("incidencias.pdf");
};
``