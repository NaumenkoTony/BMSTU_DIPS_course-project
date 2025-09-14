import React, { useState, useEffect } from "react";
import { Modal, Button, Text, Group, Paper } from "@mantine/core";
import { bookHotel, getReservation, unbookHotel, type AggregatedReservationResponse, type CreateReservationResponse } from "../api/ReservationsClient";
import "./BookHotelForm.css";
import { useNavigate } from "react-router-dom";

interface Props {
  hotelUid: string;
  hotelName?: string;
  opened: boolean;
  onClose: () => void;
  onBooked?: (resp: CreateReservationResponse) => void;
}

interface Availability {
  date: string;
  availableRooms: number;
}

export default function BookHotelForm({ hotelUid, hotelName, opened, onClose, onBooked }: Props) {
  const [startDate, setStartDate] = useState<string>("");
  const [endDate, setEndDate] = useState<string>("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reservation, setReservation] = useState<AggregatedReservationResponse | null>(null);
  const [availability, setAvailability] = useState<Availability[]>([]);

  const navigate = useNavigate();

  const reset = () => {
    setStartDate("");
    setEndDate("");
    setError(null);
    setReservation(null);
  };

  useEffect(() => {
    if (!opened) return;
    const fetchAvailability = async () => {
      try {
        const res = await fetch(`${window.appConfig?.API_URL || "http://localhost:8080/api/v1"}/hotels/${hotelUid}/availability`);
        if (!res.ok) throw new Error("Failed to fetch availability");
        const data: Availability[] = await res.json();
        setAvailability(data);
      } catch (err: any) {
        console.error(err);
      }
    };
    fetchAvailability();
  }, [hotelUid, opened]);

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    setError(null);

    if (!startDate || !endDate) {
      setError("Выберите даты заезда и выезда.");
      return;
    }
    if (startDate >= endDate) {
      setError("Дата заезда должна быть раньше даты выезда.");
      return;
    }

    const today = new Date().toISOString().split("T")[0];
    if (startDate < today) {
      setError("Дата заезда не может быть в прошлом.");
      return;
    }

    const unavailableDates = availability.filter(a => a.availableRooms === 0).map(a => a.date.split("T")[0]);
    if (unavailableDates.includes(startDate) || unavailableDates.includes(endDate)) {
      setError("Выбранная дата недоступна для бронирования.");
      return;
    }

    setLoading(true);
    try {
      const res = await bookHotel({ hotelUid, startDate, endDate });
      onBooked?.(res);
      const fullRes = await getReservation(res.reservationUid);
      setReservation(fullRes);
    } catch (err: any) {
      setError(err?.message ?? "Ошибка бронирования");
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = async () => {
    if (!reservation) return;
    try {
      await unbookHotel(reservation.reservationUid);
      reset();
      onClose();
    } catch (err: any) {
      setError(err?.message ?? "Ошибка отмены бронирования");
    }
  };

  const today = new Date().toISOString().split("T")[0];
  const unavailableDates = availability.filter(a => a.availableRooms === 0).map(a => a.date.split("T")[0]);

  const isDateDisabled = (date: string) => {
    return date < today || unavailableDates.includes(date);
  };

  return (
    <Modal
      opened={opened}
      onClose={() => { reset(); onClose(); }}
      title={`Бронирование: ${hotelName ?? hotelUid}`}
      centered
      size="md"
    >
      <Paper className="book-form" withBorder p="lg" radius="lg">
        {loading}
        {!reservation && !loading && (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="label">Дата заезда</label>
              <input
                className="date-input"
                type="date"
                value={startDate}
                min={today}
                onChange={(e) => {
                  if (!isDateDisabled(e.target.value)) setStartDate(e.target.value);
                }}
              />
            </div>

            <div className="form-group">
              <label className="label">Дата выезда</label>
              <input
                className="date-input"
                type="date"
                value={endDate}
                min={startDate || today}
                onChange={(e) => {
                  if (!isDateDisabled(e.target.value)) setEndDate(e.target.value);
                }}
              />
            </div>

            {error && <Text color="red" size="sm">{error}</Text>}

            <Group mt="xl">
              <Button variant="light" color="gray" onClick={() => { reset(); onClose(); }}>
                Отмена
              </Button>
              <Button type="submit" loading={loading}>
                Забронировать
              </Button>
            </Group>
          </form>
        )}

        {reservation && (
          <div>
            <Text size="sm" mb="sm" color="green">Бронирование подтверждено!</Text>
            <Text>Отель: <b>{reservation.hotel?.name}</b></Text>
            <Text>Город: {reservation.hotel?.fullAddress}</Text>
            <Text>Даты: {reservation.startDate} – {reservation.endDate}</Text>
            <Text>Статус: <b>{reservation.status}</b></Text>
            <Text>Стоимость: <b>{reservation.payment?.price} ₽</b></Text>

            <Group mt="xl">
              <Button variant="light" color="red" onClick={handleCancel}>
                Отменить
              </Button>
              <Button onClick={() => navigate("/reservations")}>
                Мои бронирования
              </Button>
            </Group>
          </div>
        )}
      </Paper>
    </Modal>
  );
}
