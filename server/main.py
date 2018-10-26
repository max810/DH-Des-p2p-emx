import json
import logging

import threading
from websocket_server import WebsocketServer
from User import User
from time import sleep
# from flask_cors import CORS
# from flask_socketio import SocketIO, emit
# from flask_sockets import Sockets
import base64

last_port_available = 5001

users = {
    # client_id: User
}

user_names = set()


def format_message(event, data=None):
    if data is None:
        data = {}
    message = {
        "event_type": event,
        "data": data
    }
    return json.dumps(message)


def on_new_client(client, server):
    print(client['id'], client['address'], sep='|')


def on_message(client, server, msg):
    message = json.loads(msg)
    event = message["event_type"]
    handler = handlers[event]
    handler(client, server, message)


def on_check_user_name(client, server, msg):
    data = msg['data']
    user_name = data['user_name']
    reply = 'user_name_taken' \
        if user_name in user_names \
        else 'user_name_available'
    server.send_message(client, format_message(reply))


def on_register(client, server, msg):
    global last_port_available
    client_id = client['id']
    if client_id in users.keys():
        server.send_message(client, format_message('registration_fail'))
        return
    data = msg['data']
    user_name = data['user_name']
    if user_name in user_names:
        server.send_message(client, format_message('registration_fail'))
        return
    server.send_message(
        client,
        format_message(
            'registration_success',
            {
                'port': last_port_available,
                'users': json.dumps([obj.__dict__ for obj in users.values()])
            }
        )
    )
    print(json.dumps([obj.__dict__ for obj in users.values()]))
    # address = f"ws://localhost:{last_port_available}"
    user = User(last_port_available, user_name)
    users[client_id] = user
    user_names.add(user_name)
    # increment only after everything
    last_port_available += 1

    # for client in server.clients:
    #     if client['id'] != client_id and client['id'] in users.keys():
    #         user = users[client['id']]
    #         user_name = user.user_name
    #         server.send_message(
    #             client,
    #             format_message(
    #                 'client_joined_chat',
    #                 {
    #                     'user_name': user_name,
    #                     'port': last_port_available
    #                 }
    #             )
    #         )


def on_client_left_chat(client, server):
    client_id = client['id']
    if client_id not in users.keys():
        return
    user = users[client_id]
    user_name = user.user_name

    server.send_message_to_all(
        format_message(
            'client_left_chat',
            {
                'user_name': user_name
            }
        )
    )

    user_names.remove(user_name)
    del users[client_id]


handlers = {
    "check_user_name": on_check_user_name,
    "register": on_register,
    "client_left_chat": on_client_left_chat
}

server = WebsocketServer(5000, host='127.0.0.1', loglevel=logging.DEBUG)
server.set_fn_new_client(on_new_client)
server.set_fn_message_received(on_message)
server.set_fn_client_left(on_client_left_chat)
server.run_forever()
