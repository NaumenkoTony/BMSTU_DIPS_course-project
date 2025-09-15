import React, { useState, useEffect } from "react";
import { Modal, Button, Text, Group, Paper } from "@mantine/core";
import { DateRangePicker } from 'rsuite';
import isAfter from 'date-fns/isAfter';
import dayjs from "dayjs";
import "dayjs/locale/ru";
import "rsuite/dist/rsuite.min.css";
import {
  bookHotel,
  getHotelAvailability,
  getReservation,
  unbookHotel,
  type AggregatedReservationResponse,
  type CreateReservationResponse,
} from "../api/ReservationsClient";
import { useNavigate } from "react-router-dom";
import "./BookHotelForm.css";

dayjs.locale("ru");

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

export default function BookHotelForm({
  hotelUid,
  hotelName,
  opened,
  onClose,
  onBooked,
}: Props) {
  const [range, setRange] = useState<[Date, Date] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reservation, setReservation] = useState<AggregatedReservationResponse | null>(null);
  const [availability, setAvailability] = useState<Availability[]>([]);

  const navigate = useNavigate();

  const reset = () => {
    setRange(null);
    setError(null);
    setReservation(null);
  };

  useEffect(() => {
    if (!opened) return;

    const fetchAvailability = async () => {
      try {
        const from = new Date();
        const to = dayjs().add(3, "month").toDate();
        const data = await getHotelAvailability(hotelUid, from, to);
        console.log(data);
        setAvailability(data);
      } catch (err: any) {
        console.error("Error fetching availability:", err);
      }
    };

    fetchAvailability();
  }, [hotelUid, opened]);

  const unavailableDates = availability
    .filter((a) => a.availableRooms === 0)
    .map((a) => a.date.split("T")[0]);

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    setError(null);

    if (!range) {
      setError("Выберите даты заезда и выезда.");
      return;
    }

    const [startDateObj, endDateObj] = range;

    const startDate = dayjs(startDateObj).format('YYYY-MM-DD');
    const endDate = dayjs(endDateObj).format('YYYY-MM-DD');

    if (startDate >= endDate) {
      setError("Дата заезда должна быть раньше даты выезда.");
      return;
    }

    const today = dayjs().startOf("day");
    if (dayjs(startDate).isBefore(today)) {
      setError("Дата заезда не может быть в прошлом.");
      return;
    }

    const selectedDates = [];
    let current = dayjs(startDate);
    while (current.isBefore(dayjs(endDate).add(1, "day"))) {
      selectedDates.push(current.format("YYYY-MM-DD"));
      current = current.add(1, "day");
    }

    console.log("Selected dates:", selectedDates);
    console.log("Unavailable dates:", unavailableDates);
    if (selectedDates.some((d) => unavailableDates.includes(d))) {
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

  return (
    <Modal
      opened={opened}
      onClose={() => {
        reset();
        onClose();
      }}
      title={`Бронирование: ${hotelName ?? hotelUid}`}
      centered
      size="lg"
    >
      <Paper className="book-form" withBorder p="lg" radius="lg">
        {!reservation && !loading && (
          <form onSubmit={handleSubmit}>
            <DateRangePicker
              value={range}
              onChange={setRange}
              format="dd.MM.yyyy"
              placeholder="Выберите период"
              shouldDisableDate={(date: Date) => {
                const dateStr = dayjs(date).format('YYYY-MM-DD');
                return isAfter(new Date(), date) || unavailableDates.includes(dateStr);
              }}
              style={{ width: '100%' }}
              container={() => document.body}
            />

            {error && (
              <Text color="red" size="sm" mt="md">
                {error}
              </Text>
            )}

            <Group mt="xl">
              <Button
                variant="light"
                color="gray"
                onClick={() => {
                  reset();
                  onClose();
                }}
              >
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
            <Text size="sm" mb="sm" color="green">
              Бронирование подтверждено!
            </Text>
            <Text>
              Отель: <b>{reservation.hotel?.name}</b>
            </Text>
            <Text>Город: {reservation.hotel?.fullAddress}</Text>
            <Text>
              Даты: {reservation.startDate} – {reservation.endDate}
            </Text>
            <Text>
              Статус: <b>{reservation.status}</b>
            </Text>
            <Text>
              Стоимость: <b>{reservation.payment?.price} ₽</b>
            </Text>

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